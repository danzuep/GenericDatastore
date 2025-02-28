using System;

namespace Data.Cache
{
    public class CacheEntry<T>
    {
        public T Value { get; set; }

        public long ExpiryTicks { get; set; } // Store expiry time in UTC ticks

        public bool IsExpired => DateTime.UtcNow.Ticks > ExpiryTicks;
    }
}
