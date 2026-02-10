namespace MyOrg.CurrencyConverter.API.Core.Interfaces;

/// <summary>
/// Abstraction for caching operations to allow swapping implementations (Redis, in-memory, hybrid)
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a value from the cache
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached value if found, otherwise null</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache with a time-to-live
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="ttl">Time-to-live duration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the cache service is available
    /// </summary>
    /// <returns>True if cache is available, otherwise false</returns>
    Task<bool> IsAvailableAsync();
}
