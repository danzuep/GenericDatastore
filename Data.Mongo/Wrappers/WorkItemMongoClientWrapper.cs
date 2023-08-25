using Data.Base.Models;
using Data.Base.Exceptions;
using Data.Mongo.Abstractions;
using Data.Mongo.Config;
using Data.Mongo.Models;
using Data.Mongo.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core.Misc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Data.Mongo.Wrappers
{
    /// <summary>
    /// Having a wrapper makes mocking during testing much easier for TDD.
    /// </summary>
    internal sealed class WorkItemMongoClientWrapper : IMongoClientWrapper<EntityItem>, IHealthCheck
    {
        public MongoDbOptions DbOptions { get; init; }

        private readonly Lazy<IMongoDatabase> _mongoDatabase;
        private readonly Lazy<Task<IMongoCollection<EntityItem>>> _mongoCollection;
        private readonly ILogger _logger;

        internal WorkItemMongoClientWrapper(MongoDbOptions? options, ILogger? logger = null, IMongoClient? mongoClient = null)
        {
            _logger = logger ?? NullLogger<WorkItemMongoClientWrapper>.Instance;
            mongoClient ??= Create(options?.MongoDbEndpoint);
            Ensure.IsNotNull(mongoClient, nameof(mongoClient));
            DbOptions = options ?? new();
            _mongoDatabase = new Lazy<IMongoDatabase>(() => mongoClient.GetDatabase(DbOptions.DatabaseName));
            _mongoCollection = new Lazy<Task<IMongoCollection<EntityItem>>>(() => _mongoDatabase.Value.GetOrCreateCollectionAsync<EntityItem>(DbOptions.CollectionName));
        }

        public WorkItemMongoClientWrapper(IOptionsMonitor<MongoDbOptions> options, ILogger<WorkItemMongoClientWrapper>? logger = null) : this(options.CurrentValue, logger)
        {
            if (options?.CurrentValue == null)
                throw new ArgumentNullException(nameof(options));
            var uppercaseRegion = DbOptions.DbRegion?.ToUpperInvariant() ?? string.Empty;
            var environmentRegion = string.IsNullOrEmpty(options.CurrentValue.Environment) ? uppercaseRegion : $"{uppercaseRegion} ({options.CurrentValue.Environment})";
            var recordExpiry = DbOptions.DbRecordExpiry > TimeSpan.Zero ? DbOptions.DbRecordExpiry.ToString("c") : "happen on delete";
            var mongoDbAddressParts = DbOptions.MongoDbEndpoint?.Split(":"); // mongodb://{user}:{password}@{servers}:{port}
            var mongoDbAddressPreview = mongoDbAddressParts?.Length > 1 ? string.Join(":", mongoDbAddressParts[0], mongoDbAddressParts[1]) : DbOptions.MongoDbEndpoint;
            LoggerMessage.Define<string, string, double, string?>(LogLevel.Debug, _mongoDbJobWrapperEvent,
                "Region set to {Region}, record expiry set to {Expiry}, heartbeat interval is {HeartbeatInterval:n0} ms, using MongoDb={MongoConnectString}...")
                (_logger, environmentRegion, recordExpiry, DbOptions.HeartbeatInterval.TotalMilliseconds, mongoDbAddressPreview, null);
        }

        private static readonly EventId _mongoDbJobWrapperEvent = new(1, nameof(JobQueueMongoService));

        public static MongoClient Create(string? mongoConnectString)
        {
            if (string.IsNullOrWhiteSpace(mongoConnectString))
                mongoConnectString = DatastoreConnectionOptions.LocalMongoDbEndpoint;
            var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnectString);
            var mongoClient = new MongoClient(mongoSettings);
            return mongoClient;
        }

        public async Task<Exception?> PingAsync()
        {
            Exception? error = null;
            try
            {
                await _mongoDatabase.Value.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            }
            catch (Exception ex)
            {
                _logger.Log(ex, LogLevel.Warning);
                error = ex;
            }
            return error;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthCheckResult = await PingAsync();
            var result = healthCheckResult == null ?
                HealthCheckResult.Healthy("MongoDB health check success") :
                HealthCheckResult.Unhealthy($"MongoDB health check failure: {healthCheckResult.Message}");
            return result;
        }

        #region Initialization
        private static int _isInitialized;

        public async Task<IMongoCollection<EntityItem>?> InitializeDbAsync()
        {
            IMongoCollection<EntityItem>? jobs = null;
            if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)
            {
                if (_mongoDatabase.Value == null)
                    throw new ArgumentException($"{nameof(_mongoDatabase)} is null.");
                jobs = await _mongoCollection.Value.ConfigureAwait(false);
                if (jobs != null && !(DbOptions.ReadOnlyMode > 0) &&
                    (DbOptions.OverwriteIndexes || !(await jobs.Indexes.ListAsync()).Any()))
                    await CreateJobsIndexAsync(jobs).ConfigureAwait(false);
            }
            return jobs;
        }

        private static async Task CreateJobsIndexAsync(IMongoCollection<EntityItem> mongoCollection)
        {
            await mongoCollection.Indexes.DropAllAsync().ConfigureAwait(false);

            var builder = Builders<EntityItem>.IndexKeys;

            // Create the unique compound index on Region and JobId.
            var compoundKey = builder
                .Ascending(record => record.Region)
                //.Ascending(record => record.Topic) // Beanstalkd Job IDs are unique accross tubes
                .Ascending(record => record.Id);
            var options = new CreateIndexOptions { Unique = true };
            var unique = new CreateIndexModel<EntityItem>(compoundKey, options);
            await mongoCollection.Indexes.CreateOneAsync(unique).ConfigureAwait(false);

            // Create non-unique indexes on the other fields.
            var indexes = new IndexKeysDefinition<EntityItem>[]
            {
                builder.Descending(record => record.Created),
                builder.Descending(record => record.Updated),
                builder.Ascending(record => record.Id),
                builder.Ascending(record => record.Region),
                builder.Ascending(record => record.Topic),
                builder.Ascending(record => record.State),
                builder.Ascending(record => record.OwnedBy)
            }.Select(idx => new CreateIndexModel<EntityItem>(idx));
            await mongoCollection.Indexes.CreateManyAsync(indexes).ConfigureAwait(false);

            // Calculate the "Time-To-Live" (TTL) value for the expiry index.
            var ttlExpiry = Builders<EntityItem>.IndexKeys.Ascending(record => record.Expiry);
            var expiryOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            var expiryModel = new CreateIndexModel<EntityItem>(ttlExpiry, expiryOptions);
            await mongoCollection.Indexes.CreateOneAsync(expiryModel).ConfigureAwait(false);
        }
        #endregion
    }
}

