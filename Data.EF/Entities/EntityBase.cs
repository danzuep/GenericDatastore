using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Data.EF.Entities;

internal abstract record EntityBase
{
    [Column(Order = 0)]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Key { get; set; }

    /// <summary>
    /// UTC Timestamp when the job was created.
    /// </summary>
    [Comment("The UTC creation time of the job or work item")]
    [DataType(DataType.DateTime)]
    //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC Timestamp when the job was last modified.
    /// </summary>
    [Comment("The UTC update time of the job or work item")]
    [DataType(DataType.DateTime)]
    public DateTime Updated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC Timestamp when the soft-deleted job will be permanently deleted.
    /// <example>`DateTime.UtcNow.Add(TimeSpan.FromDays(30));`</example>
    /// </summary>
    [Comment("The UTC expiry time of a soft-deleted job.")]
    public DateTime? Expiry { get; set; }

    public bool IsDeleted { get; set; }

    [NotMapped]
    public TimeSpan Duration => Updated - Created;

    [NotMapped]
    public TimeSpan? ExpiryCountdown => DateTime.UtcNow - Expiry;

    [Comment("The UserId of the person who created the record.")]
    public string? OwnedBy { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString() => $"[{Updated:s}] Key={Key}.";
}