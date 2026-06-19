using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AnagramServer2
{
    public class CacheManager
    {
        private ConcurrentDictionary<string, CacheEntry> cache =
            new ConcurrentDictionary<string, CacheEntry>();

        private ConcurrentDictionary<string, SemaphoreSlim> keyLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly TimeSpan ttl =
            TimeSpan.FromMinutes(5);

        private Thread cleanupThread;

        private bool running = true;

        public CacheManager()
        {
            cleanupThread = new Thread(CleanupLoop)
            {
                IsBackground = true,
                Name = "CacheCleanupThread"
            };

            cleanupThread.Start();
        }

        public async Task<string> GetOrAddAsync(
            string key,
            Func<Task<string>> valueFactory)
        {
            SemaphoreSlim keyLock =
                keyLocks.GetOrAdd(
                    key,
                    _ => new SemaphoreSlim(1, 1)
                );

            await keyLock.WaitAsync();

            try
            {
                if (
                    cache.TryGetValue(
                        key,
                        out var existingEntry
                    )
                    &&
                    !existingEntry.IsExpired
                )
                {
                    Logger.Log($"CACHE HIT: {key}");

                    return await existingEntry
                        .TaskFactory
                        .Value;
                }

                if (
                    existingEntry != null
                    &&
                    existingEntry.IsExpired
                )
                {
                    cache.TryRemove(
                        key,
                        out _
                    );

                    Logger.Log(
                        $"CACHE EXPIRED: {key}"
                    );
                }

                CacheEntry newEntry =
                    new CacheEntry
                    {
                        TaskFactory =
                            new Lazy<Task<string>>(
                                valueFactory
                            ),

                        Expiration =
                            DateTime.Now.Add(ttl)
                    };

                cache.AddOrUpdate(
                    key,
                    newEntry,
                    (k, old) => newEntry
                );

                Logger.Log(
                    $"CACHE MISS: {key}"
                );

                return await newEntry
                    .TaskFactory
                    .Value;
            }
            finally
            {
                keyLock.Release();
            }
        }

        private void CleanupLoop()
        {
            while (running)
            {
                try
                {
                    foreach (var pair in cache)
                    {
                        if (pair.Value.IsExpired)
                        {
                            cache.TryRemove(
                                pair.Key,
                                out _
                            );

                            Logger.Log(
                                $"CACHE REMOVE: {pair.Key}"
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(
                        $"Cache cleanup greska: {ex.Message}"
                    );
                }

                Thread.Sleep(30000);
            }
        }

        public void Stop()
        {
            running = false;

            cleanupThread?.Join();

            Logger.Log(
                "Cache cleanup thread zaustavljen."
            );
        }
    }
}