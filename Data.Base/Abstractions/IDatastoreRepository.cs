namespace Data.Base.Abstractions;

public interface IDatastoreRepository<TDal, TDto>
{
    ValueTask<IQueryable<TDal>> QueryAsync(string? filter = null);

    Task<bool> CreateAsync(TDto item, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(IEnumerable<TDal> items, CancellationToken cancellationToken = default);

    Task<TDto> ReadAsync(string id, CancellationToken cancellationToken = default);

    Task<IEnumerable<TDto>> ReadAsync(IQueryable<TDal> items, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TDto item, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(IEnumerable<TDal> items, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(TDto item, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(IQueryable<TDal> items, CancellationToken cancellationToken = default);

    IAsyncEnumerable<TDto> MonitorAsync(string id, CancellationToken cancellationToken = default);
}