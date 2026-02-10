using MyOrg.CurrencyConverter.API.Core.Models;

namespace MyOrg.CurrencyConverter.API.Services
{
    public interface ICurrencyExchangeService
    {
        Task<CurrencyRates> GetLatestRatesAsync(string baseCurrency);

        Task<decimal> ConvertCurrencyAsync(string from, string to, decimal amount);

        Task<CurrencyRates> GetExchangeRateAsync(string from, string to);

        Task<CurrencyHistoricalRates> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate);
    }
}
