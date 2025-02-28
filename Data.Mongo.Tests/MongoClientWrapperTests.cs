using System.Diagnostics;
using Data.Mongo.Wrappers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Linq;

namespace Data.Mongo.Tests
{
    [TestFixture]
    public class MongoClientWrapperTests
    {
        private readonly EntityItem[] _workItemsStub = new EntityItem[] { new() { Id = "1" } };
        private readonly IMongoClient _mongoClientMock = Substitute.For<IMongoClient>();
        private readonly WorkItemMongoClientWrapper _mongoClientWrapper;

        public MongoClientWrapperTests()
        {
            // Arrange
            var mongoCollectionMock = GetQueryableMongoCollectionMock<EntityItem>();
            var mongoDatabaseMock = Substitute.For<IMongoDatabase>();
            var mongoCursorMock = Substitute.For<IAsyncCursor<EntityItem>>();
            mongoCursorMock.Current.Returns(_workItemsStub);
            mongoCursorMock.MoveNext(Arg.Any<CancellationToken>()).Returns(true);
            mongoCursorMock.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true);
            mongoCollectionMock.Settings.Returns(new MongoCollectionSettings());
            mongoCollectionMock.AggregateAsync(Arg.Any<PipelineDefinition<EntityItem, EntityItem>>(), Arg.Any<AggregateOptions>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(mongoCursorMock);
            mongoDatabaseMock.Settings.Returns(new MongoDatabaseSettings());
            mongoDatabaseMock.GetCollection<EntityItem>(Arg.Any<string>(), null).ReturnsForAnyArgs(mongoCollectionMock);
            mongoDatabaseMock.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new BsonDocument("ok", 1));
            _mongoClientMock.Settings.Returns(Substitute.For<MongoClientSettings>());
            _mongoClientMock.Cluster.Returns(Substitute.For<ICluster>());
            _mongoClientMock.GetDatabase(Arg.Any<string>(), null).ReturnsForAnyArgs(mongoDatabaseMock);
            _mongoClientWrapper = new WorkItemMongoClientWrapper(null, null, _mongoClientMock);
        }

        public static IMongoCollection<T> GetQueryableMongoCollectionMock<T>()
        {
            var mongoCollectionMock = Substitute.For<IMongoCollection<T>>();
            var mongoDatabaseMock = Substitute.For<IMongoDatabase>();
            var mongoClientMock = Substitute.For<IMongoClient>();
            mongoClientMock.Settings.Returns(new MongoClientSettings());
            mongoDatabaseMock.Client.Returns(mongoClientMock);
            mongoCollectionMock.Database.Returns(mongoDatabaseMock);
            return mongoCollectionMock;
        }

        //public static IMongoQueryable<T> GetMongoQueryableMock<T>(IQueryable<T> queryableStub)
        //{
        //    var mongoQueryableMock = Substitute.For<IMongoQueryable<T>>();
        //    mongoQueryableMock.Provider.Returns(queryableStub.Provider);
        //    mongoQueryableMock.Expression.Returns(queryableStub.Expression);
        //    mongoQueryableMock.ElementType.Returns(queryableStub.ElementType);
        //    mongoQueryableMock.GetEnumerator().Returns(queryableStub.GetEnumerator());
        //    return mongoQueryableMock;
        //}

        [Test]
        public void AsQueryable_TestsMongoCollection_ExpectsValidCollection()
        {
            var mongoCollectionMock = GetQueryableMongoCollectionMock<EntityItem>();
            var jobsQuery = mongoCollectionMock.AsQueryable();
            Assert.That(jobsQuery, Is.Not.Null);
        }

        [Test]
        public async Task InitializeDbAsync_TestsMongoClient_ExpectsValidDatabase()
        {
            // Act
            await _mongoClientWrapper.InitializeDbAsync().ConfigureAwait(false);

            // Assert
            _mongoClientMock.Received().GetDatabase(Arg.Any<string>());
        }

        [Ignore("Dev test")]
        [TestCase("uat-nl")]
        [TestCase("uat-sg")]
        [TestCase("uat-us")]
        [TestCase("prod-hk")]
        [TestCase("prod-nl")]
        [TestCase("prod-sg")]
        [TestCase("prod-us")]
        public void TryParseRegion_TestsCiEnvironments_ExpectsValidEnvironments(string? environment)
        {
            var envVars = environment?.Split('-', StringSplitOptions.RemoveEmptyEntries);
            string regionVar = envVars?.Length > 1 ? envVars.Last() : string.Empty;
            //_ = Enum.TryParse(regionVar, ignoreCase: true, out Region region);
            Debug.WriteLine($"{environment} = {envVars?.FirstOrDefault()} {regionVar}");
        }
    }
}
