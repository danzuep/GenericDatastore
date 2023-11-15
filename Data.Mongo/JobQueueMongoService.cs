using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Data.Base.Abstractions;
using Data.Base.Exceptions;
using Data.Base.Models;
using Data.Mongo.Abstractions;
using Data.Mongo.Config;
using Data.Mongo.Models;
using Data.Mongo.Converters;
using Data.Mongo.Extensions;
using Data.Base.Extensions;

namespace Data.Mongo;

public sealed class JobQueueMongoService : IDisposable //IDatastoreRepository<WorkItem, DatastoreItem>
{
    private IMongoCollection<EntityItem>? _jobs;
    private readonly Task<IMongoCollection<EntityItem>?> _dbInitialization;
    private readonly CancellationTokenSource _cts = new();
    private readonly MongoDbOptions _dbOptions;
    private readonly ILogger _logger;

    public JobQueueMongoService(IMongoClientWrapper<EntityItem> mongoClientWrapper, ILogger<JobQueueMongoService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClientWrapper);
        _logger = logger ?? NullLogger<JobQueueMongoService>.Instance;
        _dbOptions = mongoClientWrapper.DbOptions;
        _dbInitialization = mongoClientWrapper.InitializeDbAsync();
        JobUpdates = new Lazy<IObservable<DatastoreItem>>(() =>
        {
            _ = Task.Run(WatchJobStreamLoopAsync);
            return _jobSubject;
        });
    }

    #region Change stream tracking
    public Lazy<IObservable<DatastoreItem>> JobUpdates { get; }
    private readonly Subject<DatastoreItem> _jobSubject = new();

    public async IAsyncEnumerable<DatastoreItem> MonitorAsync(string? jobId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!await InitializedAsync().ConfigureAwait(false))
            yield break;
        var cursor = await GetJobChangeCursorAsync(jobId, cancellationToken).ConfigureAwait(false);
        await foreach (var change in cursor.AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
            yield return WorkItemChangeToJobItem(change);
    }

    private async Task WatchJobStreamLoopAsync()
    {
        if (await InitializedAsync().ConfigureAwait(false))
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var cursor = await GetJobChangeCursorAsync(null, _cts.Token).ConfigureAwait(false);
                    await cursor.ForEachAsync(changeStream => HandleJobUpdate(changeStream), _cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    string message = "MongoDB change stream for jobs is unavailable";
                    _logger.Log(ex, message, LogLevel.Warning);
                    throw new InternalException(message, ex);
                }

                await Task.Delay(_dbOptions.HeartbeatInterval, _cts.Token).ConfigureAwait(false);
            }
        }
    }

    private async Task<IChangeStreamCursor<ChangeStreamDocument<EntityItem>>> GetJobChangeCursorAsync(string? jobId, CancellationToken cancellationToken = default)
    {
        _logger.Log("Starting to watch for WorkItem updates in MongoDB.");
        var builder = Builders<ChangeStreamDocument<EntityItem>>.Filter;
        var updateFilter = builder.In(change => change.OperationType, MongoDbExtensions.MonitorChangeTypes);
        var progressFilter = builder.Exists("updateDescription.updatedFields.Progress");
        var idFilter = !string.IsNullOrWhiteSpace(jobId) ?
            builder.Eq(change => change.FullDocument.Id, jobId) :
            builder.Gte(change => change.FullDocument.Updated, DateTime.Today);
        var filter = builder.And(updateFilter, progressFilter, idFilter);
        var cursor = await _jobs!.GetChangeStreamCursorAsync(filter, cancellationToken).ConfigureAwait(false);
        return cursor;
    }

    private void HandleJobUpdate(ChangeStreamDocument<EntityItem> changeStream)
    {
        try
        {
            var job = WorkItemChangeToJobItem(changeStream);
            _jobSubject.OnNext(job);
        }
        catch (Exception e)
        {
            _logger.Log(e, "Error in handling WorkItem update, subscribers were not notified.", LogLevel.Warning);
        }
    }

    private DatastoreItem WorkItemChangeToJobItem(ChangeStreamDocument<EntityItem> changeStream)
    {
        var workItem = changeStream.FullDocument;
        _logger.Log(LogLevel.Debug, "Changed WorkItem received from MongoDB, JobID {JobId} Progress={Progress}.", workItem.Id, workItem.Progress);
        return workItem.ToJobItem();
    }
    #endregion

    #region Initialization
    private async ValueTask<bool> InitializedAsync(string suffix = "")
    {
        if (_jobs == null)
        {
            var failureMessage = $"Failed to connect to the Jobs table in the database{suffix}.";
            try
            {
                _jobs = await _dbInitialization.ConfigureAwait(false);
            }
            catch (MongoConnectionException ex)
            {
                _logger.Log(ex, failureMessage, LogLevel.Error);
                throw new InternalException(failureMessage, ex);
            }
            if (_jobs == null)
            {
                _logger.Log(null, failureMessage, LogLevel.Warning);
                throw new InternalException(failureMessage);
            }
        }
        bool isInitialized = _jobs != null;
        return isInitialized;
    }
    #endregion

    public async ValueTask<IQueryable<EntityItem>> QueryAsync()
    {
        var query = await InitializedAsync().ConfigureAwait(false) ?
            _jobs.AsQueryable() : Array.Empty<EntityItem>().AsQueryable();
        return query.OrderByDescending(o => o.Created);
    }

    internal async IAsyncEnumerable<EntityItem> FetchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!await InitializedAsync().ConfigureAwait(false))
            yield break;
        var filter = Builders<EntityItem>.Filter.Empty;
        var cursor = await _jobs!.FindAsync<EntityItem>(filter, null, cancellationToken).ConfigureAwait(false);
        await foreach (var workItem in cursor.AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
            yield return workItem;
    }

    internal async Task<IAsyncCursor<EntityItem>?> FindAsync(FilterDefinition<EntityItem> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await InitializedAsync().ConfigureAwait(false);
        var results = await _jobs!.FindAsync(filter).ConfigureAwait(false);
        return results;
    }

    internal FilterDefinition<EntityItem> GetJobFilter(string jobId, string? topicName = null)
    {
        var builder = Builders<EntityItem>.Filter;
        var idFilter = builder.Eq(record => record.Id, jobId);
        var topicFilter = GetTopicFilter(builder, topicName);
        var regionFilter = GetRegionFilter(builder);
        var filter = And(builder, idFilter, topicFilter, regionFilter);
        return filter;
    }

    internal FilterDefinition<EntityItem> GetJobFilter(string? topic, string? region, IEnumerable<ActiveRestingState> states, bool includeDeleted = false)
    {
        var builder = Builders<EntityItem>.Filter;
        var topicFilter = GetTopicFilter(builder, topic);
        var regionFilter = GetRegionFilter(builder, region);
        var stateFilter = states != null && states.Any() ?
            builder.In(record => record.State, states) : includeDeleted ? null :
            builder.Ne(record => record.State, ActiveRestingState.Deleted);
        var filter = And(builder, topicFilter, regionFilter, stateFilter);
        return filter;
    }

    private static FilterDefinition<EntityItem>? GetTopicFilter(FilterDefinitionBuilder<EntityItem>? builder, string? topicName)
    {
        builder ??= Builders<EntityItem>.Filter;
        FilterDefinition<EntityItem>? topicFilter = null;
        if (!string.IsNullOrWhiteSpace(topicName))
            topicFilter = builder.Eq(record => record.Topic, topicName);
        return topicFilter;
    }

    private FilterDefinition<EntityItem>? GetRegionFilter(FilterDefinitionBuilder<EntityItem>? builder, string? region = null)
    {
        builder ??= Builders<EntityItem>.Filter;
        FilterDefinition<EntityItem>? regionFilter = null;
        if (!string.IsNullOrEmpty(region))
            regionFilter = builder.Eq(record => record.Region, region);
        else
            regionFilter = builder.Eq(record => record.Region, _dbOptions.DbRegion);
        return regionFilter;
    }

    private static FilterDefinition<T> And<T>(FilterDefinitionBuilder<T> builder, params FilterDefinition<T>?[] filters)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(filters);
        var validFilters = filters.Where(filter => filter != null);
        var filter = validFilters.Any() ? builder.And(validFilters) : builder.Empty;
        return filter;
    }

    private static readonly UpdateDefinition<EntityItem> JobDeletedUpdateDefinition =
        Builders<EntityItem>.Update.Set(x => x.State, ActiveRestingState.Deleted);

    private static readonly ReplaceOptions UpsertReplace = new() { IsUpsert = true };

    private EntityItem ChangeRegion(DatastoreItem jobItem)
    {
        jobItem.AddHistory();
        var workItem = jobItem.ToWorkItem();
        if (_dbOptions.DbRegion != null)
        {
            string topicJob = !string.IsNullOrEmpty(workItem.Topic) ? $"{workItem.Topic} job ID {workItem.Id}" : $"job ID {workItem.Id}";
            if (workItem.Region != null)
                _logger.Log("The database region has been set to {DbRegion}, but {JobId} region is set to {Region}.", _dbOptions.DbRegion, topicJob, workItem.Region, LogLevel.Warning);
            workItem.Region = _dbOptions.DbRegion;
        }
        return workItem;
    }

    public async Task<bool> CreateAsync(DatastoreItem jobItem, CancellationToken cancellationToken = default)
    {
        await InitializedAsync($" to create JobID={jobItem.Id}").ConfigureAwait(false);
        try
        {
            var workItem = ChangeRegion(jobItem);
            await _jobs!.InsertOneAsync(workItem, null, cancellationToken).ConfigureAwait(false);
        }
        catch (MongoException ex)
        {
            _logger.Log(ex, "Failed to upsert job ID={JobId}.", jobItem.Id);
        }
        return true;
    }

    public async Task<DatastoreItem> ReadAsync(string jobId, string? topicName = null, CancellationToken cancellationToken = default)
    {
        await InitializedAsync().ConfigureAwait(false);
        var filter = GetJobFilter(jobId, topicName);
        var cursor = await _jobs!.FindAsync(filter, null, cancellationToken).ConfigureAwait(false);
        var workItem = await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var jobItem = workItem?.ToJobItem() ?? new() { Id = jobId };
        return jobItem;
    }

    public async Task<bool> UpdateAsync(DatastoreItem jobItem, CancellationToken cancellationToken = default)
    {
        await InitializedAsync($" to update JobID={jobItem.Id}").ConfigureAwait(false);
        bool isSet = false;
        try
        {
            var workItem = jobItem.ToWorkItem();
            if (workItem.State == ActiveRestingState.Deleted)
            {
                _logger.Log(LogLevel.Warning, "Update method used to delete JobID {JobId}.", workItem.Id);
                if (_dbOptions.DbRecordExpiry == TimeSpan.Zero)
                {
                    var filter = GetJobFilter(workItem.Id, workItem.Topic);
                    var result = await _jobs!.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
                    isSet = result.IsAcknowledged;
                }
                else
                {
                    workItem.Expiry = DateTime.UtcNow.Add(_dbOptions.DbRecordExpiry);
                }
            }
            else
            {
                jobItem.AddHistory();
                if (long.TryParse(workItem.Id, out var jobId))
                {
                    var update = Builders<EntityItem>.Update
                        .Set(a => a.History, workItem.History)
                        .Set(a => a.Command, workItem.Command)
                        .Set(a => a.State, workItem.State)
                        .Set(a => a.Description, workItem.Description)
                        .Set(a => a.Priority, workItem.Priority)
                        .Set(a => a.DelaySeconds, workItem.DelaySeconds)
                        .Set(a => a.Progress, workItem.Progress)
                        .Set(a => a.Result, workItem.Result)
                        .Set(a => a.Error, workItem.Error)
                        .Set(a => a.Updated, DateTime.UtcNow)
                        .Set(a => a.Expiry, workItem.Expiry);
                    var filter = GetJobFilter(workItem.Id, workItem.Topic);
                    var result = await _jobs!.UpdateOneAsync(filter, update, null, cancellationToken).ConfigureAwait(false);
                    isSet = result.IsAcknowledged;
                }
                else
                {
                    var filter = GetJobFilter(workItem.Id, workItem.Topic);
                    var result = await _jobs!.ReplaceOneAsync(filter, workItem, UpsertReplace, cancellationToken).ConfigureAwait(false);
                    isSet = result.IsAcknowledged;
                }
            }
        }
        catch (MongoException ex)
        {
            _logger.Log(ex, "Failed to upsert job ID={JobId}.", jobItem.Id);
        }
        return isSet;
    }

    public async Task<bool> DeleteAsync(DatastoreItem jobItem, CancellationToken cancellationToken = default)
    {
        await InitializedAsync($" to delete JobID={jobItem.Id}").ConfigureAwait(false);
        bool isDeleted = false;
        bool permanent = _dbOptions.DbRecordExpiry == TimeSpan.Zero;
        try
        {
            var filter = GetJobFilter(jobItem.Id);
            if (permanent)
            {
                var result = await _jobs!.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
                isDeleted = result.IsAcknowledged;
            }
            else
            {
                var workItemHistory = jobItem.History?.ToWorkItemHistory();
                var updateDefinition = JobDeletedUpdateDefinition
                    .Set(x => x.Description, "Deleted")
                    .Set(x => x.History, workItemHistory)
                    .Set(x => x.Expiry, DateTime.UtcNow.Add(_dbOptions.DbRecordExpiry))
                    .Set(x => x.Updated, DateTime.UtcNow);
                var result = await _jobs!.UpdateOneAsync(filter, updateDefinition, null, cancellationToken).ConfigureAwait(false);
                isDeleted = result.IsAcknowledged;
            }
        }
        catch (MongoException ex)
        {
            _logger.Log(ex, "Failed to delete job for ID={JobId}.", jobItem.Id);
        }
        return isDeleted;
    }

    public async Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await InitializedAsync(" to bulk-delete multiple jobs").ConfigureAwait(false);
        long count = 0;
        bool permanent = _dbOptions.DbRecordExpiry == TimeSpan.Zero;
        _logger.Log("Deleting all jobs.", LogLevel.Debug);
        try
        {
            var filter = Builders<EntityItem>.Filter.Empty;
            if (permanent)
            {
                var result = await _jobs!.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);
                count = result.DeletedCount;
                var jobs = count == 1 ? "job" : "jobs";
                _logger.Log(LogLevel.Debug, "{Count} {Jobs} purged from the database.", count, jobs);
            }
            else
            {
                await DeleteExpiredAsync(cancellationToken).ConfigureAwait(false);
                var updateDefinition = JobDeletedUpdateDefinition
                    .Set(x => x.Description, "Purged")
                    .Set(x => x.Expiry, DateTime.UtcNow.Add(_dbOptions.DbRecordExpiry))
                    .Set(x => x.Updated, DateTime.UtcNow);
                var result = await _jobs!.UpdateManyAsync(filter, updateDefinition, null, cancellationToken).ConfigureAwait(false);
                count = result.MatchedCount;
                var jobs = count == 1 ? "job" : "jobs";
                _logger.Log(LogLevel.Debug, "{Count} {Jobs} marked as deleted in the database.", count, jobs);
            }
        }
        catch (MongoException ex)
        {
            _logger.Log(ex, "Failed to bulk-delete jobs.");
        }
        return count;
    }

    public async Task<bool> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        await InitializedAsync(" to delete expired jobs").ConfigureAwait(false);
        _logger.Log("Deleting all expired jobs.", LogLevel.Debug);
        bool isDeleted = false;
        try
        {
            var builder = Builders<EntityItem>.Filter;
            var expiryFilter = builder.Where(record => record.Expiry.HasValue && record.Expiry.Value > DateTime.UtcNow);
            var stateFilter = builder.Eq(record => record.State, ActiveRestingState.Deleted);
            var filter = builder.And(expiryFilter, stateFilter);
            var result = await _jobs!.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);
            _logger.Log(LogLevel.Debug, "{Count} expired job(s) deleted from the database.", result.DeletedCount);
            isDeleted = result.IsAcknowledged;
        }
        catch (MongoException ex)
        {
            _logger.Log(ex, "Failed to bulk-delete expired jobs.");
            return isDeleted;
        }
        return isDeleted;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _jobSubject.Dispose();
    }
}