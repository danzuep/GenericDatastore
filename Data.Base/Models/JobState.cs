namespace Data.Base.Models
{
    // https://google.aip.dev/216
    public enum JobState
    {
        Unknown,
        Pending,
        Running,
        Completed,
        Suspended,
        Deleted
    }
}
