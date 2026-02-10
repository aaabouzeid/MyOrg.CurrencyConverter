using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;

namespace MyOrg.CurrencyConverter.API.Services
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly ICurrencyProvider _currencyProvider;

        public CurrencyExchangeService(ICurrencyProvider currencyProvider)
        {
            _currencyProvider = currencyProvider ?? throw new ArgumentNullException(nameof(currencyProvider));
        }

        public async Task<CurrencyRates> GetLatestRatesAsync(string baseCurrency)
        {
            if (string.IsNullOrWhiteSpace(baseCurrency))
                throw new ArgumentException("Base currency cannot be empty", nameof(baseCurrency));

            return await _currencyProvider.GetLatestExchangeRates(baseCurrency);
        }

        public async Task<decimal> ConvertCurrencyAsync(string from, string to, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(from))
                throw new ArgumentException("Source currency cannot be empty", nameof(from));

            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Target currency cannot be empty", nameof(to));

            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amount));

            var rateData = await _currencyProvider.GetExchangeRate(from, to);

            if (rateData?.Rates == null || !rateData.Rates.ContainsKey(to))
                throw new InvalidOperationException($"Exchange rate for {to} not found");

            var rate = rateData.Rates[to];
            return amount * rate;
        }

        public async Task<CurrencyRates> GetExchangeRateAsync(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from))
                throw new ArgumentException("Source currency cannot be empty", nameof(from));

            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Target currency cannot be empty", nameof(to));

            return await _currencyProvider.GetExchangeRate(from, to);
        }

        public async Task<CurrencyHistoricalRates> GetHistoricalRatesAsync(
            string baseCurrency, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(baseCurrency))
                throw new ArgumentException("Base currency cannot be empty", nameof(baseCurrency));

            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date", nameof(startDate));

            if (endDate > DateTime.UtcNow.Date)
                throw new ArgumentException("End date cannot be in the future", nameof(endDate));

            return await _currencyProvider.GetHistoricalExchangeRates(baseCurrency, startDate, endDate);
        }
    }
}
