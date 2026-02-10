using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Infrastructure;
using StackExchange.Redis;

namespace MyOrg.CurrencyConverter.UnitTests.Infrastructure;

public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
    private readonly CacheSettings _cacheSettings;
    private readonly RedisCacheService _cacheService;

    public RedisCacheServiceTests()
    {
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();

        _cacheSettings = new CacheSettings
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            ThrowOnFailure = false
        };

        _mockConnectionMultiplexer
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        var options = Options.Create(_cacheSettings);

        _cacheService = new RedisCacheService(
            _mockConnectionMultiplexer.Object,
            options,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnectionMultiplexer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new RedisCacheService(
            null!,
            Options.Create(_cacheSettings),
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionMultiplexer");
    }

    [Fact]
    public void Constructor_NullCacheSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new RedisCacheService(
            _mockConnectionMultiplexer.Object,
            null!,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_KeyExists_ReturnsDeserializedValue()
    {
        // Arrange
        var key = "test:key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };
        var serializedValue = System.Text.Json.JsonSerializer.Serialize(testObject, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        _mockDatabase
            .Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)serializedValue);

        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_KeyDoesNotExist_ReturnsDefault()
    {
        // Arrange
        var key = "nonexistent:key";

        _mockDatabase
            .Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_RedisThrowsException_ReturnsDefaultWhenThrowOnFailureIsFalse()
    {
        // Arrange
        var key = "error:key";

        _mockDatabase
            .Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_RedisThrowsException_ThrowsWhenThrowOnFailureIsTrue()
    {
        // Arrange
        _cacheSettings.ThrowOnFailure = true;
        var key = "error:key";

        _mockDatabase
            .Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act & Assert
        await _cacheService.Invoking(x => x.GetAsync<TestCacheObject>(key))
            .Should().ThrowAsync<RedisConnectionException>();
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_ValidObject_SerializesAndStoresInRedis()
    {
        // Arrange
        var key = "test:key";
        var testObject = new TestCacheObject { Id = 42, Name = "Cache Test" };
        var ttl = TimeSpan.FromMinutes(30);

        _mockDatabase
            .Setup(x => x.StringSetAsync(key, It.IsAny<RedisValue>(), ttl, It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.SetAsync(key, testObject, ttl);

        // Assert
        _mockDatabase.Verify(
            x => x.StringSetAsync(
                key,
                It.Is<RedisValue>(v => v.ToString().Contains("42") && v.ToString().Contains("Cache Test")),
                ttl,
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_RedisThrowsException_DoesNotThrowWhenThrowOnFailureIsFalse()
    {
        // Arrange
        var key = "error:key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };
        var ttl = TimeSpan.FromMinutes(10);

        _mockDatabase
            .Setup(x => x.StringSetAsync(key, It.IsAny<RedisValue>(), ttl, It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var action = async () => await _cacheService.SetAsync(key, testObject, ttl);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_RedisThrowsException_ThrowsWhenThrowOnFailureIsTrue()
    {
        // Arrange
        _cacheSettings.ThrowOnFailure = true;
        var key = "error:key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };
        var ttl = TimeSpan.FromMinutes(10);

        _mockDatabase
            .Setup(x => x.StringSetAsync(key, It.IsAny<RedisValue>(), ttl, It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act & Assert
        await _cacheService.Invoking(x => x.SetAsync(key, testObject, ttl))
            .Should().ThrowAsync<RedisConnectionException>();
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_ValidKey_RemovesFromRedis()
    {
        // Arrange
        var key = "test:key";

        _mockDatabase
            .Setup(x => x.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDatabase.Verify(x => x.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_RedisThrowsException_DoesNotThrowWhenThrowOnFailureIsFalse()
    {
        // Arrange
        var key = "error:key";

        _mockDatabase
            .Setup(x => x.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var action = async () => await _cacheService.RemoveAsync(key);

        // Assert
        await action.Should().NotThrowAsync();
    }

    #endregion

    #region IsAvailableAsync Tests

    [Fact]
    public async Task IsAvailableAsync_RedisPingSucceeds_ReturnsTrue()
    {
        // Arrange
        _mockDatabase
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(10));

        // Act
        var result = await _cacheService.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_RedisPingFails_ReturnsFalse()
    {
        // Arrange
        _mockDatabase
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var result = await _cacheService.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    // Test helper class
    private class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
