using Data.Base.Models;

namespace Data.Mongo.Config
{
    public sealed record MongoDbOptions : DatabaseOptions
    {
        internal static readonly string DatabaseName = "JobQueue";
        internal static readonly string CollectionName = "WorkItems";

        internal string MongoDbEndpoint { get; set; } = DatastoreConnectionOptions.LocalMongoDbEndpoint;

        public int? ReadOnlyMode { get; set; }
    }
}
