using Data.Base.Models;

namespace Data.Base.Extensions;

public static class JobStateTypeConverter
{
    private static readonly Dictionary<JobCommand, ActiveRestingState> _jobCommandToJobStateMap = new()
    {
        { JobCommand.Unknown, ActiveRestingState.Unknown },
        { JobCommand.Create, ActiveRestingState.Pending },
        { JobCommand.Run, ActiveRestingState.Running },
        { JobCommand.Suspend, ActiveRestingState.Suspended },
        { JobCommand.Delete, ActiveRestingState.Deleted },
    };

    public static ActiveRestingState GetNextState(this JobCommand command)
    {
        _ = _jobCommandToJobStateMap.TryGetValue(command, out ActiveRestingState nextState);
        return nextState;
    }

    public static Lazy<ActiveRestingState[]> AllJobStateTypes =>
        new(() => (ActiveRestingState[])Enum.GetValues(typeof(ActiveRestingState)));

    public static Lazy<JobCommand[]> AllJobUpdateTypes =>
        new(() => (JobCommand[])Enum.GetValues(typeof(JobCommand)));

    public static readonly List<ActiveRestingState> ActiveStates = new()
    {
        ActiveRestingState.Running,
        ActiveRestingState.Pending
    };

    public static readonly List<ActiveRestingState> RestingStates = new()
    {
        ActiveRestingState.Completed,
        ActiveRestingState.Suspended,
        ActiveRestingState.Deleted
    };

    public static bool IsActiveState(this ActiveRestingState state) =>
        ActiveStates.Contains(state);

    public static bool IsRestingState(this ActiveRestingState state) =>
        RestingStates.Contains(state);
}