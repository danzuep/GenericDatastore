using Data.Base.Models;
using Data.Mongo.Models;

namespace Data.Mongo.Converters;

public static class DataAccessLayerConverter
{
    public static DatastoreItem ToJobItem(this EntityItem workItem)
    {
        var result = new DatastoreItem
        {
            Id = workItem.Id,
            State = workItem.State,
            Name = workItem.Description,
            Payload = workItem.Payload,
            Progress = workItem.Progress,
            Result = workItem.Result,
            History = workItem.History?.ToJobItemHistory()
        };
        return result;
    }

    internal static EntityItem ToWorkItem(this DatastoreItem jobItem)
    {
        var result = new EntityItem
        {
            Id = jobItem.Id,
            State = jobItem.State,
            Description = jobItem.Name,
            Payload = jobItem.Payload,
            Progress = jobItem.Progress,
            Result = jobItem.Result,
            History = jobItem.History?.ToWorkItemHistory()
        };
        return result;
    }

    // database size concern
    internal static IList<EntityItemHistory>? ToWorkItemHistory(this IEnumerable<DatastoreItemHistory>? history) =>
        history?.Select(h => h.ToWorkItemHistory()).ToList();

    // database size concern
    internal static IList<DatastoreItemHistory>? ToJobItemHistory(this IEnumerable<EntityItemHistory>? history) =>
        history?.Select(h => h.ToJobItemHistory()).ToList();

    internal static DatastoreItemHistory ToJobItemHistory(this EntityItemHistory workItemHistory)
    {
        var result = new DatastoreItemHistory
        {
            State = workItemHistory.State,
            Result = workItemHistory.Result,
            Updated = workItemHistory.Updated
        };
        return result;
    }

    internal static EntityItemHistory ToWorkItemHistory(this DatastoreItemHistory jobItemHistory)
    {
        var result = new EntityItemHistory
        {
            State = jobItemHistory.State,
            Result = jobItemHistory.Result,
            Updated = jobItemHistory.Updated
        };
        return result;
    }
}