using Microsoft.Extensions.Options;
using MyOrg.CurrencyConverter.API.Core.Enums;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;

namespace MyOrg.CurrencyConverter.API.Infrastructure;

/// <summary>
/// Factory for creating currency provider instances based on configuration
/// Implements the Factory Pattern to support multiple provider implementations
/// </summary>
public class CurrencyProviderFactory : ICurrencyProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CurrencyProviderSettings _settings;
    private readonly ILogger<CurrencyProviderFactory> _logger;

    public CurrencyProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<CurrencyProviderSettings> settings,
        ILogger<CurrencyProviderFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ICurrencyProvider CreateProvider(CurrencyProviderType providerType)
    {
        _logger.LogInformation("Creating currency provider of type: {ProviderType}", providerType);

        return providerType switch
        {
            CurrencyProviderType.Frankfurter => CreateFrankfurterProvider(),

            _ => throw new NotSupportedException(
                $"Currency provider type '{providerType}' is not yet implemented. " +
                $"Currently supported: {string.Join(", ", GetAvailableProviders())}")
        };
    }

    /// <inheritdoc/>
    public ICurrencyProvider CreateProvider()
    {
        var activeProvider = _settings.ActiveProvider;
        _logger.LogInformation("Creating default currency provider: {ActiveProvider}", activeProvider);
        return CreateProvider(activeProvider);
    }

    /// <inheritdoc/>
    public IEnumerable<CurrencyProviderType> GetAvailableProviders()
    {
        // Return only implemented providers
        return
        [
            CurrencyProviderType.Frankfurter
            // Add more as they are implemented
        ];
    }

    private ICurrencyProvider CreateFrankfurterProvider()
    {
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        return new FrankfurterCurrencyProvider(httpClientFactory);
    }
}
