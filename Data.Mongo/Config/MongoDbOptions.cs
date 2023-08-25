using Data.Base.Models;

namespace Data.Mongo.Config
{
    public sealed record MongoDbOptions : DatabaseOptions
    {
        internal string MongoDbEndpoint { get; set; } = DatastoreConnectionOptions.LocalMongoDbEndpoint;

        public int? ReadOnlyMode { get; set; }

        public bool OverwriteIndexes { get; set; }

        public string DatabaseName { get; set; } = "JobQueue";

        public string CollectionName { get; set; } = "WorkItems";
    }
}
