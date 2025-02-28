using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Data.Cache
{
    public sealed class CacheService : ICacheService
    {
        private readonly CacheOptions _defaultOptions = new CacheOptions();
        private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, long> _cacheExpiry = new ConcurrentDictionary<string, long>();
        private readonly ConcurrentDictionary<string, long> _cacheLastUsed = new ConcurrentDictionary<string, long>();
        private readonly ILogger<CacheService> _logger;

        public CacheService(ILogger<CacheService> logger = null)
        {
            _logger = logger;
        }

        public T Add<T>(string key, Func<T> factory, CacheOptions cacheOptions = null)
        {
            // Validate the key
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            // Use default cache options if none provided
            if (cacheOptions == null)
            {
                cacheOptions = _defaultOptions;
            }

            // Create a composite key if necessary
            var cacheKey = cacheOptions.UseCompositeKey ?
                string.Join(cacheOptions.Delimiter, typeof(T).FullName, key) : key;

            // Add a new entry if not in cache
            return Add(factory, cacheOptions, DateTime.UtcNow, cacheKey, null);
        }

        public T GetOrAdd<T>(string key, Func<T> factory, CacheOptions cacheOptions = null)
        {
            // Validate the key
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            // Use default cache options if none provided
            if (cacheOptions == null)
            {
                cacheOptions = _defaultOptions;
            }

            // Create a composite key if necessary
            var cacheKey = cacheOptions.UseCompositeKey ?
                string.Join(cacheOptions.Delimiter, typeof(T).FullName, key) : key;

            var utcNow = DateTime.UtcNow;

            // Check if the value is already in cache
            if (_cache.TryGetValue(cacheKey, out var cacheEntry))
            {
                // Check if the entry has been used recently
                if (utcNow.Ticks < _cacheLastUsed[cacheKey] + cacheOptions.Expiry.Ticks)
                {
                    _cacheLastUsed[cacheKey] = utcNow.Ticks; // Update last used time
                    return (T)cacheEntry;
                }

                // Launch a task to update the expired cache entry, but return the existing entry
                _ = RenewAsync(factory, cacheOptions, utcNow, cacheKey, cacheEntry);
                return (T)cacheEntry; // Return stale data while renewing
            }

            if (TryWaitForExistingRenewal<T>(cacheKey, cacheOptions, out cacheEntry)) // Wait for renewal if in progress
            {
                return (T)cacheEntry;
            }

            // Add a new entry if not in cache
            return Add(factory, cacheOptions, utcNow, cacheKey, null);
        }

        private async Task RenewAsync<T>(Func<T> factory, CacheOptions cacheOptions, DateTime utcNow, string cacheKey, object cacheEntry)
        {
            await Task.CompletedTask; // Ensure the method is async
            try
            {
                // Renew the cache entry
                Add(factory, cacheOptions, utcNow, cacheKey, cacheEntry);
                // Remove expired entries after renewal
                RemoveExpired(cacheOptions.Expiry.Ticks);
            }
            catch (Exception ex)
            {
                _logger?.Log(ex, "Failed to renew cache entry.");
            }
        }

        private bool TryWaitForExistingRenewal<T>(string cacheKey, CacheOptions cacheOptions, out object value)
        {
            var retryCount = 0;
            // Check if the entry is currently being renewed
            while (retryCount < cacheOptions.RetryLimit && _cacheExpiry.TryGetValue(cacheKey, out _))
            {
                // Wait for the renewal to complete
                Task.Delay(cacheOptions.Delay).Wait();
                // Check if the value is now in cache
                if (_cache.TryGetValue(cacheKey, out var cacheEntry))
                {
                    value = cacheEntry;
                    return true;
                }
                retryCount++;
            }
            value = default;
            return false;
        }

        private T Add<T>(Func<T> factory, CacheOptions cacheOptions, DateTime utcNow, string cacheKey, object cacheEntry)
        {
            _cacheExpiry[cacheKey] = utcNow.Add(cacheOptions.Expiry).Ticks; // Set expiry time
            var updatedEntry = factory(); // Generate new cache entry
            _cacheLastUsed[cacheKey] = utcNow.Ticks; // Set last used time
            if (updatedEntry != null && cacheEntry == null)
            {
                _cache[cacheKey] = updatedEntry;
                return updatedEntry;
            }
            else if (updatedEntry != null &&
                ((cacheEntry == null && _cache.TryAdd(cacheKey, updatedEntry)) ||
                    _cache.TryUpdate(cacheKey, updatedEntry, cacheEntry)))
            {
                return updatedEntry;
            }
            _logger?.Log("Failed add or update the cache entry.");
            _cacheExpiry[cacheKey] = utcNow.Ticks; // instant expiry
            return (T)cacheEntry; // Return old entry if update fails
        }

        internal int RemoveExpired(long expiryTicks)
        {
            // Check if there are any last used entries to process
            if (_cacheLastUsed.IsEmpty)
            {
                return 0;
            }

            var utcNow = DateTime.UtcNow;
            int removed = 0;

            // Remove expired entries
            foreach (var cacheKey in _cacheLastUsed.Keys)
            {
                // Check if the entry is expired
                if (utcNow.Ticks >= _cacheExpiry[cacheKey] &&
                    utcNow.Ticks >= _cacheLastUsed[cacheKey] + expiryTicks)
                {
                    _cache.TryRemove(cacheKey, out _);
                    _cacheExpiry.TryRemove(cacheKey, out _);
                    _cacheLastUsed.TryRemove(cacheKey, out _);
                    removed++;
                }
            }

            return removed;
        }
    }
}
