using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;

namespace MyOrg.CurrencyConverter.API.Infrastructure
{
    public class FrankfurterCurrencyProvider : ICurrencyProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string HttpClientName = "FrankfurterApi";

        public FrankfurterCurrencyProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<CurrencyRates> GetLatestExchangeRates(string baseCurrency)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var url = $"latest?from={baseCurrency}";

            var response = await client.GetFromJsonAsync<CurrencyRates>(url);

            if (response == null)
                throw new InvalidOperationException("Failed to retrieve currency rates from Frankfurter API");

            return response;
        }

        public async Task<CurrencyRates> GetExchangeRate(string baseCurrency, string targetCurrency)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var url = $"latest?from={baseCurrency}&to={targetCurrency}";

            var response = await client.GetFromJsonAsync<CurrencyRates>(url);

            if (response == null)
                throw new InvalidOperationException("Failed to retrieve specific currency rates from Frankfurter API");

            return response;
        }


        public async Task<CurrencyHistoricalRates> GetHistoricalExchangeRates(string baseCurrency, DateTime startDate, DateTime endDate)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var formattedStartDate = startDate.ToString("yyyy-MM-dd");
            var formattedEndDate = endDate.ToString("yyyy-MM-dd");

            var url = $"{formattedStartDate}..{formattedEndDate}?from={baseCurrency}";
            var response = await client.GetFromJsonAsync<CurrencyHistoricalRates>(url);

            if (response == null)
                throw new InvalidOperationException("Failed to retrieve historical currency rates from Frankfurter API");

            return response;
        }
    }
}