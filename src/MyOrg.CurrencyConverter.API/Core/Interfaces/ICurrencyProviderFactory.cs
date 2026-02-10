using MyOrg.CurrencyConverter.API.Core.Enums;

namespace MyOrg.CurrencyConverter.API.Core.Interfaces;

/// <summary>
/// Factory for creating currency provider instances
/// </summary>
public interface ICurrencyProviderFactory
{
    /// <summary>
    /// Creates a currency provider based on the specified type
    /// </summary>
    /// <param name="providerType">The type of provider to create</param>
    /// <returns>An instance of ICurrencyProvider</returns>
    ICurrencyProvider CreateProvider(CurrencyProviderType providerType);

    /// <summary>
    /// Creates a currency provider based on the configured default provider
    /// </summary>
    /// <returns>An instance of ICurrencyProvider</returns>
    ICurrencyProvider CreateProvider();

    /// <summary>
    /// Gets all available provider types
    /// </summary>
    /// <returns>Collection of available provider types</returns>
    IEnumerable<CurrencyProviderType> GetAvailableProviders();
}
