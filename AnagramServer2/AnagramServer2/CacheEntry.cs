using System;
using System.Threading.Tasks;

namespace AnagramServer2
{
    public class CacheEntry
    {
        public required Lazy<Task<string>> TaskFactory { get; set; }

        public DateTime Expiration { get; set; }

        public bool IsExpired => DateTime.Now > Expiration;
    }
}