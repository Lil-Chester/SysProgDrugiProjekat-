using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AnagramServer2
{
    public class CacheManager
    {
        private ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();
        private readonly TimeSpan ttl = TimeSpan.FromMinutes(5);

        public Task<string> GetOrAddAsync(string key, Func<Task<string>> valueFactory)
        {
            if (cache.TryGetValue(key, out var existingEntry) && existingEntry.IsExpired)
            {
                cache.TryRemove(key, out _);
            }

            var entry = cache.GetOrAdd(key, k =>
            {
                Logger.Log($"CACHE MISS: {k}");
                return new CacheEntry
                {
                    TaskFactory = new Lazy<Task<string>>(valueFactory),
                    Expiration = DateTime.Now.Add(ttl)
                };
            });

            if (entry.IsExpired)
            {
                cache.TryRemove(key, out _);
                entry = cache.GetOrAdd(key, k => new CacheEntry
                {
                    TaskFactory = new Lazy<Task<string>>(valueFactory),
                    Expiration = DateTime.Now.Add(ttl)
                });
            }
            else
            {
                if (entry.TaskFactory.IsValueCreated)
                {
                    Logger.Log($"CACHE HIT: {key}");
                }
            }

            return entry.TaskFactory.Value;
        }
    }
}