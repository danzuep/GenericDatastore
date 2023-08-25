using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Data.EF.Extensions;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class EntityFrameworkExtensions
{
    public static DbContextOptions<TContext> GetNpgsqlContextOptions<TContext>(string connectionString) where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>()
            //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        return builder.Options;
    }

    public static IQueryable<T> QueryAsNoTracking<T>(this DbContext context) where T : class
    {
        return context.Set<T>().AsNoTracking();
    }

    public static IQueryable<T> QueryWhere<T>(this DbContext context, Expression<Func<T, bool>> predicate) where T : class
    {
        return context.Set<T>().Where(predicate);
    }

    public static async Task UpdateOrAddAsync<T>(this DbContext context, T? data, T? existing = null, CancellationToken cancellationToken = default) where T : class
    {
        if (context != null && data != null)
        {
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(data);
            }
            else
            {
                await context.AddAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public static async Task<int> UpdateOrAddAsync<T>(this DbContext context, T? data, IQueryable<T>? existingItems, CancellationToken cancellationToken = default) where T : class
    {
        int updateCount = 0;
        if (context != null && data != null)
        {
            if (existingItems != null)
            {
                foreach (var item in existingItems)
                {
                    await context.UpdateOrAddAsync(data, item, cancellationToken).ConfigureAwait(false);
                    updateCount++;
                }
            }
            else
            {
                await context.AddAsync(data, cancellationToken).ConfigureAwait(false);
                updateCount++;
            }
        }
        return updateCount;
    }
}
