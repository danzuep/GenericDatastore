using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Data.Cache.Tests
{
    public class CacheServiceTests
    {
        private static readonly CacheOptions _options =
            new CacheOptions { Expiry = TimeSpan.FromMilliseconds(1) };
        private readonly CacheService _cacheService;
        private readonly Mock<ILogger<CacheService>> _loggerMock;

        public CacheServiceTests()
        {
            _loggerMock = new Mock<ILogger<CacheService>>();
            _cacheService = new CacheService(_loggerMock.Object);
        }

        [Fact]
        public void GetOrAdd_ShouldAddAndRetrieveItem()
        {
            // Arrange
            string key = "testKey";
            string expectedValue = "testValue";
            Func<string> factory = () => expectedValue;

            // Act
            var result = _cacheService.GetOrAdd(key, factory, _options);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void GetOrAdd_ShouldReturnExistingItemBeforeExpiry()
        {
            // Arrange
            string key = "testKey";
            string expectedValue = "testValue";
            Func<string> factory = () => expectedValue;

            // Add value
            _cacheService.GetOrAdd(key, factory, _options);

            // Act
            var result = _cacheService.GetOrAdd(key, factory, _options); // Should return the existing value

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void Add_WhenCalledTwice_ShouldReturnSecondItem()
        {
            // Arrange
            string key = "testKey";
            string initialValue = "initialValue";
            string newValue = "newValue";
            Func<string> factory = () => newValue;

            // Add initial value
            _cacheService.Add(key, () => initialValue, _options);

            // Add new value
            var result = _cacheService.Add(key, factory, _options);

            // Assert that the renewed value is returned
            Assert.Equal(newValue, result);
        }

        [Fact]
        public void RemoveExpired_ShouldRemoveExpiredEntry()
        {
            // Arrange
            string key = "testKey";
            string expectedValue = "testValue";
            Func<string> factory = () => expectedValue;

            // Add initial value
            _cacheService.Add(key, factory, _options);

            // Act
            var result = _cacheService.RemoveExpired(TimeSpan.Zero.Ticks);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void GetOrAdd_ShouldHandleNullKey()
        {
            // Arrange
            Func<string> factory = () => "value";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _cacheService.GetOrAdd(null, factory));
            Assert.Equal("Key cannot be null or empty. (Parameter 'key')", exception.Message);
        }
    }
}
