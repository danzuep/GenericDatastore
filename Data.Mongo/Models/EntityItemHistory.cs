using Data.Base.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Mongo.Models;

[BsonIgnoreExtraElements]
public sealed record EntityItemHistory
{
    public DateTime Updated { get; set; }

    public JobState State { get; set; }

    public string? Result { get; set; }

    public override string ToString() => $"[{Updated:s}] State={State}.";
}
