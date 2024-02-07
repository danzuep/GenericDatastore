using Data.Base.Abstractions;
using Data.Base.Models;
using Data.EF.Abstractions;
using Data.EF.Converters;
using Data.EF.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Data.EF
{
    internal sealed class EfRepositoryService : IDatastoreRepository<DatastoreItem>, IDisposable
    {
        private readonly IServiceScope _serviceScope;
        private readonly IRepositoryWrapper<EntityItem> _efWrapper;

        public EfRepositoryService(IServiceProvider serviceProvider)
        {
            _serviceScope = serviceProvider.CreateScope();
            _efWrapper = _serviceScope.ServiceProvider.GetRequiredService<IRepositoryWrapper<EntityItem>>();
        }

        public async ValueTask<IQueryable<EntityItem>> QueryAsync(string? filter = null)
        {
            return await _efWrapper.QueryAsync();
        }

        public async Task<int> CreateAsync(DatastoreItem item, CancellationToken cancellationToken = default)
        {
            var entity = item.ToJobEntity();
            return await _efWrapper.CreateAsync(entity, cancellationToken);
        }

        public async Task<int> CreateAsync(IEnumerable<DatastoreItem> items, CancellationToken cancellationToken = default)
        {
            var entities = items.ToJobEntity();
            return await _efWrapper.CreateAsync(entities, cancellationToken);
        }

        public async Task<DatastoreItem?> ReadAsync(string id, CancellationToken cancellationToken = default)
        {
            var entities = await _efWrapper.QueryAsync();
            return entities.SingleOrDefault(o => o.Id == id)?.ToJobItem();
        }

        public async Task<IEnumerable<DatastoreItem>> ReadAsync(IQueryable<DatastoreItem> items, CancellationToken cancellationToken = default)
        {
            var entities = await _efWrapper.QueryAsync();
            return entities.ToJobItem().Intersect(items);
        }

        public async Task<int> UpdateAsync(DatastoreItem item, CancellationToken cancellationToken = default)
        {
            var entity = item.ToJobEntity();
            return await _efWrapper.UpdateAsync(entity, cancellationToken);
        }

        public async Task<int> UpdateAsync(IEnumerable<DatastoreItem> items, CancellationToken cancellationToken = default)
        {
            var entity = items.ToJobEntity();
            return await _efWrapper.UpdateAsync(entity, cancellationToken);
        }

        public async Task<int> DeleteAsync(DatastoreItem item, CancellationToken cancellationToken = default)
        {
            var entity = item.ToJobEntity();
            return await _efWrapper.DeleteAsync(entity, cancellationToken);
        }

        public async Task<int> DeleteAsync(IQueryable<DatastoreItem> items, CancellationToken cancellationToken = default)
        {
            var entity = items.ToJobEntity();
            return await _efWrapper.DeleteAsync(entity, cancellationToken);
        }

        public void Dispose()
        {
            _serviceScope.Dispose();
        }
    }
}
