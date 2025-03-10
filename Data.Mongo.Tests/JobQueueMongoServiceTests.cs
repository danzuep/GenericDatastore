﻿global using Data.Mongo.Models;
global using NSubstitute;
global using NUnit.Framework;
using Data.Base.Models;
using Data.Mongo.Abstractions;
using Data.Mongo.Config;

namespace Data.Mongo.Tests
{
    [TestFixture]
    public sealed class JobQueueMongoServiceTests : IDisposable
    {
        private static readonly string _testId = "1";
        private static readonly string _testTopic = "Topic";
        private static readonly DatastoreItem _jobItemStub = new() { Id = _testId };
        private readonly JobQueueMongoService _dbService;

        public JobQueueMongoServiceTests()
        {
            // Arrange
            var mongoCollectionMock = MongoClientWrapperTests.GetQueryableMongoCollectionMock<EntityItem>();
            var mongoClientWrapperMock = Substitute.For<IMongoClientWrapper<EntityItem>>();
            mongoClientWrapperMock.DbOptions.Returns(new MongoDbOptions());
            mongoClientWrapperMock.InitializeDbAsync().Returns(mongoCollectionMock);
            _dbService = new JobQueueMongoService(mongoClientWrapperMock);
        }

        [Test]
        public async Task CreateReadUpdateDeleteAsync_TestsCRUD_ExpectsValidEntityItem()
        {
            // Arrange
            var jobItem = _jobItemStub;
            var cancellationToken = CancellationToken.None;

            // Act
            await _dbService.CreateAsync(jobItem, cancellationToken);
            var result = await _dbService.ReadAsync(_testId, _testTopic, cancellationToken);
            await _dbService.UpdateAsync(jobItem, cancellationToken);
            await _dbService.DeleteAsync(jobItem, cancellationToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.AssignableFrom<DatastoreItem>());
        }

        [Test]
        public async Task DeleteAsync_TestsBulkDelete_ExpectsOne()
        {
            var result = await _dbService.DeleteAsync(_jobItemStub, CancellationToken.None);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task QueryAsync_TestsValidTopic_ExpectsValidEntityItem()
        {
            var workItems = await _dbService.QueryAsync().ConfigureAwait(false);
            Assert.That(workItems, Is.Not.Null);
        }

        [Test]
        public async Task MonitorProgressAsync_TestsChangeStream_ExpectsValidJobItem()
        {
            // Act
            await foreach (var jobItem in _dbService.MonitorAsync(_testId, CancellationToken.None).ConfigureAwait(false))
            {
                // Assert
                Assert.That(jobItem, Is.Not.Null);
                Assert.That(jobItem, Is.AssignableFrom<DatastoreItem>());
                break;
            }
        }

        //[Ignore("TODO - add fakes to the the mongoClientWrapperMock")]
        //[Test]
        //public async Task JobUpdates_TestsChangeStream_ExpectsValidJobItem()
        //{
        //    // Act
        //    await foreach (var jobItem in _dbService.JobUpdates.Value.ToAsyncEnumerable().ConfigureAwait(false))
        //    {
        //        // Assert
        //        Assert.That(jobItem, Is.Not.Null);
        //        Assert.That(jobItem, Is.AssignableFrom<DatastoreItem>());
        //        break;
        //    }
        //}

        public void Dispose()
        {
            _dbService.Dispose();
        }
    }
}
