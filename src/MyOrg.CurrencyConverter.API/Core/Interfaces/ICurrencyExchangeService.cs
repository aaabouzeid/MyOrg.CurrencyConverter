using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Core.Models.Requests;
using MyOrg.CurrencyConverter.API.Core.Models.Responses;

namespace MyOrg.CurrencyConverter.API.Services
{
    public interface ICurrencyExchangeService
    {
        Task<CurrencyRates> GetLatestRatesAsync(GetLatestRatesRequest request);
        Task<decimal> ConvertCurrencyAsync(ConvertCurrencyRequest request);
        Task<CurrencyRates> GetExchangeRateAsync(GetExchangeRateRequest request);
        Task<PagedResult<PagedHistoricalRatesResponse>> GetHistoricalRatesAsync(GetHistoricalRatesRequest request);
    }
}
