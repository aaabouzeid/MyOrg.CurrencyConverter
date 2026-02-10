using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Core.Models.Requests;

namespace MyOrg.CurrencyConverter.API.Services
{
    public interface ICurrencyExchangeService
    {
        Task<CurrencyRates> GetLatestRatesAsync(GetLatestRatesRequest request);
        Task<decimal> ConvertCurrencyAsync(ConvertCurrencyRequest request);
        Task<CurrencyRates> GetExchangeRateAsync(GetExchangeRateRequest request);
        Task<CurrencyHistoricalRates> GetHistoricalRatesAsync(GetHistoricalRatesRequest request);
    }
}
