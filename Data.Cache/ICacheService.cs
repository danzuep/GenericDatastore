using System;

namespace Data.Cache
{
    public interface ICacheService
    {
        T Add<T>(string key, Func<T> factory, CacheOptions cacheOptions = null);
        T GetOrAdd<T>(string key, Func<T> factory, CacheOptions cacheOptions = null);
    }
}
