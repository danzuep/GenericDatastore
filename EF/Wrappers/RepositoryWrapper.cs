using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Data.Base.Exceptions;
using Data.Base.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Data.EF.Abstractions;
using Data.EF.Entities;
using Data.EF.Extensions;

namespace Data.EF.Wrappers;

[ExcludeFromCodeCoverage]
internal sealed class RepositoryWrapper<T> : IRepositoryWrapper<T> where T : EntityBase, new()
{
    private readonly ILogger _logger;
    private readonly DbContext _dbContext;
    private readonly Task _dbInitialization = Task.CompletedTask;

    /// <summary>
    /// Creates a new RepositoryWrapper using the supplied DbContext.
    /// </summary>
    public RepositoryWrapper(DbContext dbContext, ILogger<RepositoryWrapper<T>>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? NullLogger<RepositoryWrapper<T>>.Instance;
        _dbInitialization = InitializeDbAsync();
    }

    private static int _isInitialized;

    private async Task InitializeDbAsync()
    {
        bool recreate = false;
        if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)
        {
            var dbPath = Path.Combine(Environment.CurrentDirectory, DatastoreConnectionOptions.LocalDatabase);
            if (File.Exists(dbPath))
            {
                _logger.Log(LogLevel.Debug, "Scheduler database already exists: {DbPath}.", dbPath);
            }
            else
            {
                recreate = true;
                _logger.Log(LogLevel.Information, "Creating a new Scheduler database: {DbPath}.", dbPath);
            }
            try
            {
                await InitializeAsync(recreate).ConfigureAwait(false);
            }
            catch (SqliteException ex)
            {
                await CaptureAsync(ex, recreate).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                await CaptureAsync(ex, recreate).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                await CaptureAsync(ex, recreate).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                _logger.Log(ex, "{ExceptionMessage}{NewLine}{CustomMessage}",
                    ex.Message, Environment.NewLine, $"Close any programs that access {DatastoreConnectionOptions.LocalDatabase}, e.g. DB Browser.");
                System.Diagnostics.Debugger.Break();
            }
            catch (Exception ex)
            {
                _logger.Log(ex, ex.Message);
                System.Diagnostics.Debugger.Break();
                throw new InternalException(ex);
            }
        }
    }

    /// <summary>
    /// Capture schema exception in DEBUG mode, e.g.
    /// InvalidOperationException, DbUpdateException, SqliteException.
    /// </summary>
    private async Task CaptureAsync(Exception ex, bool recreate)
    {
        _logger.Log(ex, recreate ? LogLevel.Error : LogLevel.Warning);
        System.Diagnostics.Debugger.Break();
        if (recreate == false)
        {
            _logger.Log("Recreating Scheduler database.", LogLevel.Debug);
            await _dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await _dbContext.Database.OpenConnectionAsync().ConfigureAwait(false);
            await _dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
            //await MigrateAsync().ConfigureAwait(false);
        }
    }

    public async Task InitializeAsync(bool recreate, CancellationToken cancellationToken = default)
    {
        if (recreate)
        {
            await _dbContext.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        }
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Database.MigrateAsync(cancellationToken);

    public async Task<int> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (!_dbInitialization.IsCompleted)
            await _dbInitialization.ConfigureAwait(false);
        _ = await _dbContext.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        var count = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    public async ValueTask<IQueryable<T>> QueryAsync()
    {
        if (!_dbInitialization.IsCompleted)
            await _dbInitialization.ConfigureAwait(false);
        var entities = _dbContext.Set<T>()
            .OrderByDescending(a => a.Key);
        return entities;
    }

    public async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var entities = await QueryAsync().ConfigureAwait(false);
        var matches = entities.Where(a => a.Key == entity.Key);
        _ = await _dbContext.UpdateOrAddAsync(entity, matches, cancellationToken).ConfigureAwait(false);
        var count = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    public async Task<int> UpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        if (!_dbInitialization.IsCompleted)
            await _dbInitialization.ConfigureAwait(false);
        var items = entities.ToList();
        if (items.Count > 0)
        {
            _dbContext.UpdateRange(items);
            _ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return items.Count;
    }

    public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (!_dbInitialization.IsCompleted)
            await _dbInitialization.ConfigureAwait(false);
        _ = _dbContext.Remove(entity);
        var count = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    public async Task<int> DeleteAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        if (!_dbInitialization.IsCompleted)
            await _dbInitialization.ConfigureAwait(false);
        var items = entities.ToList();
        if (items.Count > 0)
        {
            _dbContext.RemoveRange(items);
            _ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return items.Count;
    }

    public async Task WithinTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        try
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await action().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _logger.Log(ex, LogLevel.Warning);
        }
        catch (DbException ex)
        {
            _logger.Log(ex, LogLevel.Error);
        }
    }
}
