using Microsoft.Extensions.Options;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;

namespace MyOrg.CurrencyConverter.API.Infrastructure;

/// <summary>
/// Decorator that adds caching to currency provider operations using cache-aside pattern
/// </summary>
public class CachedCurrencyProvider : ICurrencyProvider
{
    private readonly ICurrencyProvider _innerProvider;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<CachedCurrencyProvider> _logger;

    public CachedCurrencyProvider(
        ICurrencyProvider innerProvider,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<CachedCurrencyProvider> logger)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CurrencyRates> GetLatestExchangeRates(string baseCurrency)
    {
        // Check if caching is enabled
        if (!_cacheSettings.Enabled)
        {
            _logger.LogDebug("Caching disabled, calling provider directly for GetLatestExchangeRates");
            return await _innerProvider.GetLatestExchangeRates(baseCurrency);
        }

        var cacheKey = GenerateCacheKey("latest", baseCurrency.ToUpperInvariant());

        try
        {
            // Try to get from cache
            var cachedRates = await _cacheService.GetAsync<CurrencyRates>(cacheKey);

            if (cachedRates != null)
            {
                _logger.LogInformation("Returning cached latest rates for base currency: {BaseCurrency}", baseCurrency);
                return cachedRates;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache retrieval failed for key: {CacheKey}, falling back to provider", cacheKey);
        }

        // Cache miss - call inner provider
        _logger.LogDebug("Cache miss for latest rates, calling provider for base currency: {BaseCurrency}", baseCurrency);
        var rates = await _innerProvider.GetLatestExchangeRates(baseCurrency);

        // Store in cache
        try
        {
            var ttl = TimeSpan.FromMinutes(_cacheSettings.Ttl.LatestRatesMinutes);
            await _cacheService.SetAsync(cacheKey, rates, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache latest rates for key: {CacheKey}", cacheKey);
        }

        return rates;
    }

    /// <inheritdoc/>
    public async Task<CurrencyRates> GetExchangeRate(string baseCurrency, string targetCurrency)
    {
        // Check if caching is enabled
        if (!_cacheSettings.Enabled)
        {
            _logger.LogDebug("Caching disabled, calling provider directly for GetExchangeRate");
            return await _innerProvider.GetExchangeRate(baseCurrency, targetCurrency);
        }

        var cacheKey = GenerateCacheKey("pair", baseCurrency.ToUpperInvariant(), targetCurrency.ToUpperInvariant());

        try
        {
            // Try to get from cache
            var cachedRate = await _cacheService.GetAsync<CurrencyRates>(cacheKey);

            if (cachedRate != null)
            {
                _logger.LogInformation("Returning cached exchange rate for {BaseCurrency}/{TargetCurrency}", baseCurrency, targetCurrency);
                return cachedRate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache retrieval failed for key: {CacheKey}, falling back to provider", cacheKey);
        }

        // Cache miss - call inner provider
        _logger.LogDebug("Cache miss for exchange rate, calling provider for {BaseCurrency}/{TargetCurrency}", baseCurrency, targetCurrency);
        var rate = await _innerProvider.GetExchangeRate(baseCurrency, targetCurrency);

        // Store in cache
        try
        {
            var ttl = TimeSpan.FromMinutes(_cacheSettings.Ttl.ExchangeRateMinutes);
            await _cacheService.SetAsync(cacheKey, rate, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache exchange rate for key: {CacheKey}", cacheKey);
        }

        return rate;
    }

    /// <inheritdoc/>
    public async Task<(CurrencyHistoricalRates rates, int totalDays)> GetHistoricalExchangeRates(
        string baseCurrency,
        DateTime startDate,
        DateTime endDate,
        int pageNumber,
        int pageSize)
    {
        // No caching for historical data due to pagination complexity and lower benefit
        _logger.LogDebug("No caching for historical rates, calling provider directly");
        return await _innerProvider.GetHistoricalExchangeRates(baseCurrency, startDate, endDate, pageNumber, pageSize);
    }

    /// <summary>
    /// Generates a cache key with consistent format to avoid collisions
    /// </summary>
    /// <param name="operation">Operation type (e.g., "latest", "pair")</param>
    /// <param name="parameters">Operation parameters in uppercase</param>
    /// <returns>Cache key in format: currency:{operation}:{param1}:{param2}</returns>
    private static string GenerateCacheKey(string operation, params string[] parameters)
    {
        var keyParts = new[] { "currency", operation }.Concat(parameters);
        return string.Join(":", keyParts);
    }
}
