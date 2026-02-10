namespace MyOrg.CurrencyConverter.API.Core.Models.Responses;

/// <summary>
/// Paginated response for historical exchange rates
/// </summary>
public class PagedHistoricalRatesResponse
{
    /// <summary>
    /// The base currency code
    /// </summary>
    public string Base { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the range (ISO format: yyyy-MM-dd)
    /// </summary>
    public string StartDate { get; set; } = string.Empty;

    /// <summary>
    /// End date of the range (ISO format: yyyy-MM-dd)
    /// </summary>
    public string EndDate { get; set; } = string.Empty;

    /// <summary>
    /// Exchange rates by date
    /// Key: date in ISO format (yyyy-MM-dd)
    /// Value: dictionary of currency codes to exchange rates
    /// </summary>
    public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }

    public PagedHistoricalRatesResponse()
    {
        Rates = new Dictionary<string, Dictionary<string, decimal>>();
    }

    /// <summary>
    /// Creates a PagedHistoricalRatesResponse from CurrencyHistoricalRates
    /// </summary>
    /// <param name="currencyHistoricalRates">The source data from the provider</param>
    /// <returns>A new PagedHistoricalRatesResponse instance</returns>
    public static PagedHistoricalRatesResponse FromCurrencyHistoricalRates(CurrencyHistoricalRates currencyHistoricalRates)
    {
        return new PagedHistoricalRatesResponse
        {
            Base = currencyHistoricalRates.Base,
            StartDate = currencyHistoricalRates.StartDate,
            EndDate = currencyHistoricalRates.EndDate,
            Rates = currencyHistoricalRates.Rates
        };
    }
}
