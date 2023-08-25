using System.ComponentModel.DataAnnotations;

namespace Data.Base.Models
{
    public sealed record DatastoreConnectionOptions
    {
        public const string DefaultMongoHost = "localhost";
        public const string DefaultMongoPort = "27017";
        public const string LocalMongoDbEndpoint = $"mongodb://{DefaultMongoHost}:{DefaultMongoPort}"; //"mongodb://${MONGO_USER}:${MONGO_PASSWORD}@${MONGO_HOST}:${MONGO_PORT}"
        public static readonly string LocalPostgreSqlEndpoint = "host=host.docker.internal port=5432"; //dbname=${PG_NAME} user=${PG_USER} password=${PG_PASSWORD}
        public static readonly string LocalDatabaseName = "LocalDatabase";
        public static readonly string LocalDatabase = $"{LocalDatabaseName}.db";
        public static readonly string LocalSqLiteEndpoint = $"Data Source={LocalDatabase}";
        public static readonly string LocalSqLiteInMemoryEndpoint = "DataSource=:memory:";

        [Required(ErrorMessage = "Datastore connection string is missing.")]
        public string DatastoreEndpoint { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}