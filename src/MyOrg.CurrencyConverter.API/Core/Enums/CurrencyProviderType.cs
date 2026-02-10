namespace MyOrg.CurrencyConverter.API.Core.Enums;

/// <summary>
/// Types of currency exchange rate providers
/// </summary>
public enum CurrencyProviderType
{
    Invalid = 0,

    /// <summary>
    /// Frankfurter API (https://www.frankfurter.app/)
    /// </summary>
    Frankfurter = 1,
}
