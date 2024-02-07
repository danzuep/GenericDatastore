namespace Data.Base.Models
{
    public class DatabaseOptions
    {
        public string Environment { get; set; } = string.Empty;

        public DatastoreType Type { get; set; }

        public bool UseDatastore => Type == DatastoreType.InMemory ||
            !string.IsNullOrWhiteSpace(ConnectionStrings.DatastoreEndpoint);

        public string? DbRegion { get; set; }

        public TimeSpan DbRecordExpiry { get; set; } = TimeSpan.Zero;

        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

        public DatastoreConnectionOptions ConnectionStrings { get; set; } = new();
    }
}
