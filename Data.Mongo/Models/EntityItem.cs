using System.ComponentModel.DataAnnotations;
using Data.Base.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Mongo.Models;

[BsonIgnoreExtraElements]
public sealed record EntityItem
{
    /// <summary>
    /// Server region.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Work queue topic or Beanstalkd tube name.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Job ID or Beanstalkd job number.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public string? Command { get; set; }

    public JobState State { get; set; }

    public string? Description { get; set; }

    public string Payload { get; set; } = string.Empty;

    [Range(0, 100)]
    public byte? Progress { get; set; }

    public string? Result { get; set; }

    /// <summary>
    /// Last error message, usually blank.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Job priority, 1 is highest.
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Maximum time the job can run without interaction.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TimeToRun { get; set; } = 60;

    [BsonIgnore]
    public TimeSpan Runtime => TimeSpan.FromSeconds(TimeToRun);

    /// <summary>
    /// Delay time before the job will start.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? DelaySeconds { get; set; }

    [BsonIgnore]
    public TimeSpan? Delay => DelaySeconds > 0 ? TimeSpan.FromSeconds(DelaySeconds.Value) : null;

    /// <summary>
    /// UTC Timestamp when the job was created.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC Timestamp when the job was last modified.
    /// </summary>
    public DateTime Updated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC Timestamp when the soft-deleted job will be permanently deleted.
    /// <example>`DateTime.UtcNow.Add(TimeSpan.FromDays(30));`</example>
    /// </summary>
    public DateTime? Expiry { get; set; }

    [BsonIgnore]
    public TimeSpan Duration => Updated - Created;

    [BsonIgnore]
    public TimeSpan? ExpiryCountdown => DateTime.UtcNow - Expiry;

    /// <summary>
    /// The UserId of the person who created the work item.
    /// </summary>
    public string? OwnedBy { get; set; }

    public IList<EntityItemHistory>? History { get; set; }

    public override string ToString()
    {
        var result = $"[{Updated:s}] ID={Id}, State={State}, Topic={Topic}.";
        return result;
    }
}