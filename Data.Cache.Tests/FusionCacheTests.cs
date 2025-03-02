using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Data.Cache.Tests
{
    public class FusionCacheTests
    {
        private readonly HybridCache _cache;

        public FusionCacheTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFusionCache().AsHybridCache();
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            _cache = scope.ServiceProvider.GetRequiredService<HybridCache>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public async Task GetOrCreateAsync_ShouldAddAndRetrieveItemAsync(int id)
        {
            var expected = $"product:{id}";
            var result = await _cache.GetOrCreateAsync($"{id}", (ct) => ValueTask.FromResult(expected));
            Assert.Equal(expected, result);
        }
    }
}
