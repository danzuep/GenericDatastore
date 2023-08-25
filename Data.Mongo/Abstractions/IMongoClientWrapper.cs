using Data.Mongo.Config;
using MongoDB.Driver;

namespace Data.Mongo.Abstractions
{
    public interface IMongoClientWrapper<T>
    {
        MongoDbOptions DbOptions { get; }

        Task<IMongoCollection<T>?> InitializeDbAsync();
    }
}