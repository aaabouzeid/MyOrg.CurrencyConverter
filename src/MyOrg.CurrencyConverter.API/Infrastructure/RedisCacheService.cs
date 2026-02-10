using System.Text.Json;
using Microsoft.Extensions.Options;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;
using StackExchange.Redis;

namespace MyOrg.CurrencyConverter.API.Infrastructure;

/// <summary>
/// Redis implementation of the cache service abstraction
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<CacheSettings> cacheSettings,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
            {
                _logger.LogWarning("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogInformation("Cache hit for key: {Key}", key);
            var deserializedValue = JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
            return deserializedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value from cache for key: {Key}", key);

            if (_cacheSettings.ThrowOnFailure)
            {
                throw;
            }

            return default;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);

            await db.StringSetAsync(key, serializedValue, ttl);
            _logger.LogInformation("Cached value for key: {Key} with TTL: {TTL}s", key, ttl.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);

            if (_cacheSettings.ThrowOnFailure)
            {
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
            _logger.LogInformation("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);

            if (_cacheSettings.ThrowOnFailure)
            {
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.PingAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection is not available");
            return false;
        }
    }
}
