using MyOrg.CurrencyConverter.API.Core.DTOs.Requests;
using MyOrg.CurrencyConverter.API.Core.DTOs.Responses;
using MyOrg.CurrencyConverter.API.Core.Models;

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
