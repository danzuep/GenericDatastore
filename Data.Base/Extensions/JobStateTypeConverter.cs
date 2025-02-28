using Data.Base.Models;

namespace Data.Base.Extensions;

public static class JobStateTypeConverter
{
    private static readonly Dictionary<JobCommand, JobState> _jobCommandToJobStateMap = new()
    {
        { JobCommand.Unknown, JobState.Unknown },
        { JobCommand.Create, JobState.Pending },
        { JobCommand.Run, JobState.Running },
        { JobCommand.Suspend, JobState.Suspended },
        { JobCommand.Delete, JobState.Deleted },
    };

    public static JobState GetNextState(this JobCommand command)
    {
        _ = _jobCommandToJobStateMap.TryGetValue(command, out JobState nextState);
        return nextState;
    }

    public static Lazy<JobState[]> AllJobStateTypes =>
        new(() => Enum.GetValues<JobState>());

    public static Lazy<JobCommand[]> AllJobUpdateTypes =>
        new(() => Enum.GetValues<JobCommand>());

    public static readonly List<JobState> ActiveStates = new()
    {
        JobState.Running,
        JobState.Pending
    };

    public static readonly List<JobState> RestingStates = new()
    {
        JobState.Completed,
        JobState.Suspended,
        JobState.Deleted
    };

    public static bool IsActiveState(this JobState state) =>
        ActiveStates.Contains(state);

    public static bool IsRestingState(this JobState state) =>
        RestingStates.Contains(state);
}
