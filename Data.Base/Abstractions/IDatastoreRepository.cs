namespace Data.Base.Abstractions;

public interface IDatastoreRepository<TDto>
{
    Task<int> CreateAsync(TDto item, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(IEnumerable<TDto> items, CancellationToken cancellationToken = default);

    Task<TDto?> ReadAsync(string id, CancellationToken cancellationToken = default);

    Task<IEnumerable<TDto>> ReadAsync(IQueryable<TDto> items, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(TDto item, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(IEnumerable<TDto> items, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(TDto item, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(IQueryable<TDto> items, CancellationToken cancellationToken = default);

    //IAsyncEnumerable<TDto> MonitorAsync(string id, CancellationToken cancellationToken = default);
}