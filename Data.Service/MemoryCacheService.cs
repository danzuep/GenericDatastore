using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace Data.Service
{
    public sealed class MemoryCacheService<T> : ICacheService<T>
    {
        private readonly CacheOptions _options;
        private readonly IMemoryCache _cache;

        public MemoryCacheService(CacheOptions? options = null, IMemoryCache? memoryCache = null)
        {
            _options = options ?? new();
            _cache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            _options.Prefix = _options.AddPrefix ? typeof(T).FullName ?? throw new ArgumentException("The generic type parameter name is null.") : string.Empty;
        }

        public T? GetOrAdd(string key, Func<T> factory)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            var cacheKey = _options.AddPrefix ? Join([_options.Prefix, key]) : key;

            if (!_cache.TryGetValue(key, out T? result))
            {
                result = AddCacheEntry(cacheKey, factory);
            }

            return result;
        }

        private T? AddCacheEntry(string cacheKey, Func<T> factory)
        {
            var isInUse = Join([cacheKey, _options.Suffix]);
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(_options.Expiry);
            var cacheRenewalOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_options.Renewal)
                .RegisterPostEvictionCallback(RenewalCallback);
            _cache.Set(isInUse, null as object, cacheRenewalOptions);
            var result = factory();
            if (result != null)
            {
                _cache.Set(cacheKey, result, cacheEntryOptions);
            }
            return result;

            void RenewalCallback(object evictedKey, object? value, EvictionReason reason, object? state)
            {
                if (reason == EvictionReason.Expired && evictedKey is string key) // && !_cache.TryGetValue(evictedKey, out _))
                {
                    Debug.WriteLine($"Renewing cache entry for {key}");
                    var renewalKey = Join(Split(key).SkipLast(1));
                    _ = AddCacheEntry(renewalKey, factory);
                }
            }
        }

        private string Join(IEnumerable<string> values) => string.Join(_options.Delimiter, values);

        private string[] Split(string value) => value.Split(_options.Delimiter, StringSplitOptions.None);
    }
}
