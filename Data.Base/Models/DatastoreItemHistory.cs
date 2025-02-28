namespace Data.Base.Models;

public sealed record DatastoreItemHistory
{
    public DateTime Updated { get; set; }

    public JobState State { get; set; }

    public string? Result { get; set; }

    public override string ToString() => $"[{Updated:s}] State={State}.";
}
