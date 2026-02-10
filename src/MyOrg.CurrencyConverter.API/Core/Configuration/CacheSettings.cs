namespace MyOrg.CurrencyConverter.API.Core.Configuration;

/// <summary>
/// Configuration settings for caching
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Whether caching is enabled (feature flag)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Redis connection string (e.g., "localhost:6379" or "redis.azure.com:6380,ssl=true,password=xxx")
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Default TTL in minutes for cache entries (fallback if specific TTL not set)
    /// </summary>
    public int DefaultTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Specific TTL settings for different cache operations
    /// </summary>
    public CacheTtlSettings Ttl { get; set; } = new();

    /// <summary>
    /// Whether to throw exceptions on cache failures (false = graceful degradation)
    /// </summary>
    public bool ThrowOnFailure { get; set; } = false;
}

/// <summary>
/// TTL settings for specific cache operations
/// </summary>
public class CacheTtlSettings
{
    /// <summary>
    /// TTL in minutes for latest exchange rates
    /// </summary>
    public int LatestRatesMinutes { get; set; } = 30;

    /// <summary>
    /// TTL in minutes for single exchange rates (currency pair)
    /// </summary>
    public int ExchangeRateMinutes { get; set; } = 30;
}
