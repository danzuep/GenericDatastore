global using Moq;
using Data.Base.Models;
using Data.EF.Abstractions;
using Data.EF.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Data.EF.Tests
{
    public class EfRepositoryServiceTests : IDisposable
    {
        #region Setup
        private static readonly EntityItem[] StubItems = [
            new() { Id = EntityItem.GetHexId(4), Description = "Apple", TimeToRun = 60 },
            new() { Id = EntityItem.GetHexId(4), Description = "Orange", TimeToRun = 30 },
            new() { Id = EntityItem.GetHexId(4), Description = "Strawberry", TimeToRun = 15 },
        ];

        private readonly EfRepositoryService _efRepositoryService;

        public EfRepositoryServiceTests()
        {
            // Arrange
            _efRepositoryService = CreateTest();
        }

        private static EfRepositoryService CreateTest()
        {
            var serviceCollection = new ServiceCollection()
                .AddLogging(o => o.SetMinimumLevel(LogLevel.None));
            serviceCollection.AddEntityFrameworkDatabase(DatastoreType.InMemory, null);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var efRepositoryService = new EfRepositoryService(serviceProvider);
            return efRepositoryService;
        }

        private static EfRepositoryService CreateMock(IQueryable<EntityItem> entityItems)
        {
            var mockEfWrapper = new Mock<IRepositoryWrapper<EntityItem>>();
            mockEfWrapper.Setup(m => m.QueryAsync()).ReturnsAsync(entityItems);
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(RepositoryDbContext))).Returns(new RepositoryDbContext());
            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(serviceScopeMock.Object);
            var efRepositoryService = new EfRepositoryService(serviceProviderMock.Object);
            return efRepositoryService;
        }

        [Fact(Skip = "not tested")]
        public async Task GetMockRecord_ReturnFailAsync()
        {
            using var repository = CreateMock(StubItems.AsQueryable());
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _efRepositoryService.QueryAsync(It.IsAny<string>()));
            var item = await repository.ReadAsync(StubItems[0].Id);
            Assert.Null(item);
            //Assert.Equal(StubItems[0].Id, item?.Id);
        }
        #endregion

        [Theory]
        [InlineData("1")]
        public async Task GetDatasetRecordAsync(string id)
        {
            var stub = new DatastoreItem() { Id = id };
            await _efRepositoryService.CreateAsync(stub);
            var item = await _efRepositoryService.ReadAsync(id);
            Assert.NotNull(item);
            Assert.Equal(stub.Id, item.Id);
            //Assert.Equal(stub, item);
        }

        public void Dispose()
        {
            _efRepositoryService.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}