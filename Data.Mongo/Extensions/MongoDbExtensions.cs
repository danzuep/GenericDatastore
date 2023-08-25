using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.CompilerServices;

namespace Data.Mongo.Extensions;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class MongoDbExtensions
{
    private static readonly Task _completedTask = Task.CompletedTask;

    public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T>? values, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (values == null)
            yield break;

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return value;
        }

        await _completedTask;
    }

    internal static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IAsyncCursor<T>? cursor, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (cursor == null)
            yield break;

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return document;
            }
        }
    }

    internal static async Task<IMongoCollection<T>> GetOrCreateCollectionAsync<T>(this IMongoDatabase database, string collectionName, CreateCollectionOptions? options = null)
    {
        var listCollectionOptions = new ListCollectionsOptions { Filter = new BsonDocument("name", collectionName) };
        var cursor = await database.ListCollectionsAsync(listCollectionOptions).ConfigureAwait(false);
        if (!await cursor.AnyAsync().ConfigureAwait(false))
            await database.CreateCollectionAsync(collectionName, options).ConfigureAwait(false);
        return database.GetCollection<T>(collectionName);
    }

    internal static readonly List<ChangeStreamOperationType> MonitorChangeTypes = new()
    {
        ChangeStreamOperationType.Update,
        ChangeStreamOperationType.Replace,
        ChangeStreamOperationType.Delete
    };

    internal static async Task<IChangeStreamCursor<ChangeStreamDocument<T>>> GetChangeStreamCursorAsync<T>(this IMongoCollection<T> collection, FilterDefinition<ChangeStreamDocument<T>> filter, CancellationToken cancellationToken = default)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>().Match(filter);
        var options = new ChangeStreamOptions() { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
        var cursor = await collection!.WatchAsync(pipeline, options, cancellationToken).ConfigureAwait(false);
        return cursor;
    }

}
