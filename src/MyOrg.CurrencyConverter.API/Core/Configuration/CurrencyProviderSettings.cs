using MyOrg.CurrencyConverter.API.Core.Enums;

namespace MyOrg.CurrencyConverter.API.Core.Configuration;

/// <summary>
/// Configuration settings for currency providers
/// </summary>
public class CurrencyProviderSettings
{
    /// <summary>
    /// The active provider type to use
    /// </summary>
    public CurrencyProviderType ActiveProvider { get; set; } = CurrencyProviderType.Frankfurter;

    /// <summary>
    /// Frankfurter API configuration
    /// </summary>
    public FrankfurterProviderSettings Frankfurter { get; set; } = new();
}

/// <summary>
/// Frankfurter API specific settings
/// </summary>
public class FrankfurterProviderSettings
{
    /// <summary>
    /// Base URL for the Frankfurter API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.frankfurter.app";
}
