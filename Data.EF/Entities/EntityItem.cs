﻿using Data.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Data.EF.Entities;

[Table("Items", Schema = "dbo")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Region), nameof(Topic), nameof(Id), nameof(State))]
internal sealed record EntityItem : EntityBase
{
    [Column(Order = 1)]
    [MaxLength(50)]
    [Display(Name = "Job ID")]
    [Comment("The job ID or work instance number")]
    public string Id { get; set; } = string.Empty;

    [Column(Order = 2)]
    [Comment("Server region.")]
    public string? Region { get; set; }

    [Column(Order = 3)]
    [MaxLength(50)]
    [Comment("The topic name of the job queue or work tube")]
    public string Topic { get; set; } = string.Empty;

    [Column(Order = 4)]
    [Comment("The last command for the job or work item")]
    public string? Command { get; set; }

    [Column(Order = 5)]
    [Comment("The state of the job or work item")]
    public JobState State { get; set; }

    [Column(Order = 6)]
    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    [Comment("The payload of the job or work item")]
    public string Payload { get; set; } = string.Empty;

    [Range(0, 100)]
    public byte? Progress { get; set; }

    [MaxLength(1000)]
    [Comment("The result of the job or work item")]
    public string? Result { get; set; }

    [MaxLength(1000)]
    public string? Error { get; set; }

    public int Priority { get; set; } = 1;

    [Range(0, int.MaxValue)]
    public int TimeToRun { get; set; } = 60;

    [Range(0, int.MaxValue)]
    public int? DelaySeconds { get; set; }

    /// <summary>
    /// Truncated hexadecimal string with up to 16^32 combinations.
    /// </summary>
    /// <param name="count">Base 16 exponent.</param>
    /// <returns>Truncated hexadecimal string</returns>
    public static string GetHexId(byte count = 32)
    {
        if (count > 32)
            count = 32;
        var uuid = Guid.NewGuid().ToString("n", null)[..count];
        return uuid;
    }

    [ExcludeFromCodeCoverage]
    public override string ToString() => $"Key={Key}, Region={Region}, Name={Topic}, ID={Id}, State={State}, Payload=\"{Payload}\".";
}