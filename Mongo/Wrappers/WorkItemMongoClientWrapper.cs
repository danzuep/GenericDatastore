using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core.Misc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Data.Base.Models;
using Data.Base.Exceptions;
using Data.Mongo.Abstractions;
using Data.Mongo.Config;
using Data.Mongo.Models;
using Data.Mongo.Extensions;

namespace Data.Mongo.Wrappers
{
    /// <summary>
    /// Having a wrapper makes mocking during testing much easier for TDD.
    /// </summary>
    internal sealed class WorkItemMongoClientWrapper : IMongoClientWrapper<EntityItem>
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
            _mongoDatabase = new Lazy<IMongoDatabase>(() => mongoClient.GetDatabase(MongoDbOptions.DatabaseName));
            _mongoCollection = new Lazy<Task<IMongoCollection<EntityItem>>>(() => _mongoDatabase.Value.GetOrCreateCollectionAsync<EntityItem>(MongoDbOptions.CollectionName));
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
            var mongoClient = new MongoClient(mongoConnectString);
            return mongoClient;
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                await _mongoDatabase.Value.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(ex, LogLevel.Warning);
                return false;
            }
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
                if (jobs != null && !(DbOptions.ReadOnlyMode > 0))
                    await CreateJobsIndexAsync(jobs).ConfigureAwait(false);
            }
            return jobs;
        }

        private static async Task CreateJobsIndexAsync(IMongoCollection<EntityItem> mongoCollection)
        {
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
                builder.Ascending(record => record.Region),
                builder.Ascending(record => record.Id),
                builder.Ascending(record => record.Topic),
                builder.Ascending(record => record.State),
                builder.Descending(record => record.Created),
                builder.Descending(record => record.Updated),
                builder.Descending(record => record.Expiry),
                builder.Ascending(record => record.OwnedBy)
            }.Select(idx => new CreateIndexModel<EntityItem>(idx));
            await mongoCollection.Indexes.CreateManyAsync(indexes).ConfigureAwait(false);
        }
        #endregion
    }
}

