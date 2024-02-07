using Data.EF.Entities;

namespace Data.EF.Abstractions;

internal interface IRepositoryWrapper<TDal> where TDal : EntityBase, new()
{
    ValueTask<IQueryable<TDal>> QueryAsync(); //string? filter = null

    Task<int> CreateAsync(TDal entity, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(IEnumerable<TDal> entities, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(TDal entity, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(IEnumerable<TDal> entities, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(TDal entity, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(IEnumerable<TDal> entities, CancellationToken cancellationToken = default);
}