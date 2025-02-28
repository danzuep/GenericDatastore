using System;

namespace Data.Cache
{
    public sealed class CacheOptions
    {
        public TimeSpan Expiry { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan Delay { get; set; } = TimeSpan.Zero;
        public int RetryLimit { get; set; }
        public bool UseCompositeKey { get; set; } = true;
        public string Delimiter { get; set; } = "&";
    }
}
