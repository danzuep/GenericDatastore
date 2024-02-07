using Data.EF.Entities;
using Data.Base.Models;
using Data.Base.Extensions;

namespace Data.EF.Converters;

public static class DataAccessLayerConverter
{
    internal static DatastoreItem ToJobItem(this EntityItem entity)
    {
        var result = new DatastoreItem
        {
            Id = entity.Id,
            State = entity.State,
            Name = entity.Description,
            Payload = entity.Payload,
            Progress = entity.Progress,
            Result = entity.Result,
        };
        return result;
    }

    internal static IEnumerable<DatastoreItem> ToJobItem(this IEnumerable<EntityItem> entities) =>
        entities.Select(m => m.ToJobItem());

    internal static EntityItem ToJobEntity(this DatastoreItem model)
    {
        var result = new EntityItem
        {
            Id = model.Id.LengthCheck(50),
            Description = model.Name?.LengthCheck(500),
            Payload = model.Payload.LengthCheck(1000),
            Progress = model.Progress,
            Result = model.Result?.LengthCheck(1000),
        };
        result.Updated = DateTime.UtcNow;
        return result;
    }

    internal static IEnumerable<EntityItem> ToJobEntity(this IEnumerable<DatastoreItem> models) =>
        models.Select(m => m.ToJobEntity());
}