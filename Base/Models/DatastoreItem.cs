using System.ComponentModel.DataAnnotations;

namespace Data.Base.Models;

public sealed record DatastoreItem
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string Payload { get; set; } = string.Empty;

    public ActiveRestingState State { get; set; }

    [Range(0, 100)]
    public byte? Progress { get; set; }

    public string? Result { get; set; }

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

    public IList<DatastoreItemHistory>? History { get; set; }

    public override string ToString() => $"ID={Id}";
}