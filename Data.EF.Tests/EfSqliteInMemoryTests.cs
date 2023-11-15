global using Xunit;
using System.Data.Common;
using Data.EF.Entities;
using Data.EF.Wrappers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Data.EF.Tests
{
    // https://github.com/dotnet/EntityFramework.Docs/blob/main/samples/core/Testing/TestingWithoutTheDatabase/SqliteInMemoryBloggingControllerTest.cs
    public class EfSqliteInMemoryTests : IDisposable
    {
        #region Constructor and dispose
        private readonly DbConnection _connection;
        private readonly DbContextOptions<RepositoryDbContext> _contextOptions;

        public EfSqliteInMemoryTests()
        {
            // Create and open a connection. This creates the SQLite in-memory database, which will persist until the connection is closed
            // at the end of the test (see Dispose below).
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // These options will be used by the context instances in this test suite, including the connection opened above.
            _contextOptions = new DbContextOptionsBuilder<RepositoryDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create the schema and seed some data
            using var context = TestContext;
            context.Database.EnsureCreated();

            context.AddRange(_workItemsStub);
            context.SaveChanges();
        }

        RepositoryDbContext TestContext => new RepositoryDbContext(_contextOptions);

        static RepositoryWrapper<EntityItem> TestWrapper(DbContext dbContext) => new RepositoryWrapper<EntityItem>(dbContext);

        public void Dispose()
        {
            _connection.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion

        private static readonly EntityItem _workItemStub = new() { Id = "1" };
        private static readonly EntityItem[] _workItemsStub = [_workItemStub];

        [Fact (Skip = "not tested")]
        public async Task GetEntityItemByIdAsync()
        {
            using var context = TestContext;
            var repository = TestWrapper(context);

            var entities = await repository.QueryAsync();
            var matches = entities.Where(a => a.Id == _workItemStub.Id);
            var item = matches.FirstOrDefault();

            Assert.NotNull(item?.Id);
            Assert.Equal(_workItemStub.Id, item.Id);
        }

        [Fact(Skip = "not tested")]
        public async Task AddEntityItemAsync()
        {
            using var context = TestContext;
            var repository = TestWrapper(context);

            var itemStub = new EntityItem() { Id = "1" };
            var count = await repository.CreateAsync(itemStub);

            Assert.True(count > 0);
        }

        [Fact(Skip = "not tested")]
        public async Task UpdateEntityItemAsync()
        {
            using var context = TestContext;
            var repository = TestWrapper(context);

            var itemStub = _workItemStub with { Region = "new" };
            var count = await repository.UpdateAsync(itemStub);

            var item = context.Jobs.Single(b => b.Region == itemStub.Region);
            Assert.NotNull(item?.Id);
            Assert.Equal(_workItemStub.Id, item.Id);
        }
    }
}