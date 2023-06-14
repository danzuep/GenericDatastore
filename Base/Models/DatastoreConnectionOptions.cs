using System.ComponentModel.DataAnnotations;

namespace Data.Base.Models
{
    public sealed record DatastoreConnectionOptions
    {
        public static readonly string LocalDatabaseName = "LocalDatabase";
        public static readonly string LocalDatabase = $"{LocalDatabaseName}.db";
        public static readonly string LocalSqLiteEndpoint = $"Data Source={LocalDatabase}";
        public static readonly string LocalSqLiteInMemoryEndpoint = "DataSource=:memory:";
        public static readonly string LocalMongoDbEndpoint = "mongodb://localhost:27017"; //"mongodb://${MONGO_USER}:${MONGO_PASSWORD}@${MONGO_HOST}:${MONGO_PORT}"
        public static readonly string LocalPostgreSqlEndpoint = "host=host.docker.internal port=5432"; //dbname=${PG_NAME} user=${PG_USER} password=${PG_PASSWORD}

        [Required(ErrorMessage = "Datastore connection string is missing.")]
        public string DatastoreEndpoint { get; set; } = string.Empty;
    }
}