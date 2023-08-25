using Data.EF.Entities;

namespace Data.EF.Abstractions;

internal interface IRepositoryWrapper<T> where T : EntityBase, new()
{
    ValueTask<IQueryable<T>> QueryAsync();

    Task<int> CreateAsync(T entity, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
}