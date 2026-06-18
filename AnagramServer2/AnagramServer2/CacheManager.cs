using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AnagramServer2
{
    public class CacheManager
    {
        private ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();
        private ConcurrentDictionary<string, SemaphoreSlim> keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly TimeSpan ttl = TimeSpan.FromMinutes(5);

        public async Task<string> GetOrAddAsync(string key, Func<Task<string>> valueFactory)
        {
            SemaphoreSlim keyLock = keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await keyLock.WaitAsync();
            try
            {
                if (cache.TryGetValue(key, out var existingEntry) && !existingEntry.IsExpired)
                {
                    if (existingEntry.TaskFactory.IsValueCreated)
                    {
                        Logger.Log($"CACHE HIT: {key}");
                    }
                    return await existingEntry.TaskFactory.Value;
                }

                if (existingEntry != null && existingEntry.IsExpired)
                {
                    cache.TryRemove(key, out _);
                    Logger.Log($"CACHE EXPIRED: {key}");
                }

                var newEntry = new CacheEntry
                {
                    TaskFactory = new Lazy<Task<string>>(valueFactory),
                    Expiration = DateTime.Now.Add(ttl)
                };

                var entry = cache.AddOrUpdate(key, newEntry, (k, old) => newEntry);
                Logger.Log($"CACHE MISS: {key}");

                return await entry.TaskFactory.Value;
            }
            finally
            {
                keyLock.Release();
            }
        }
    }
}