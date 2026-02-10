using MyOrg.CurrencyConverter.API.Core.Models;

namespace MyOrg.CurrencyConverter.API.Core.Interfaces
{
    public interface ICurrencyProvider
    {
        Task<CurrencyRates> GetLatestExchangeRates(string baseCurrency);

        Task<CurrencyRates> GetExchangeRate(string baseCurrency, string targetCurrency);

        Task<(CurrencyHistoricalRates rates, int totalDays)> GetHistoricalExchangeRates(string baseCurrency, DateTime startDate, DateTime endDate, int pageNumber, int pageSize);
    }
}
