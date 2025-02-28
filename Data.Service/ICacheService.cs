
namespace Data.Service
{
    public interface ICacheService<T>
    {
        T? GetOrAdd(string key, Func<T> factory);
    }
}
