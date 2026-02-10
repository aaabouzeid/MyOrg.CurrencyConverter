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


        public async Task<(CurrencyHistoricalRates rates, int totalDays)> GetHistoricalExchangeRates(string baseCurrency, DateTime startDate, DateTime endDate, int pageNumber, int pageSize)
        {
            // Calculate total days in the original date range
            var totalDays = (endDate - startDate).Days + 1;

            // Calculate the date range for the requested page
            int skip = (pageNumber - 1) * pageSize;

            // If the page is beyond the available range, return empty result
            if (skip >= totalDays)
            {
                var emptyResult = new CurrencyHistoricalRates
                {
                    Base = baseCurrency,
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd"),
                    Rates = new Dictionary<string, Dictionary<string, decimal>>()
                };
                return (emptyResult, totalDays);
            }

            // Adjust the date range to fetch only the dates for this page
            var adjustedStartDate = startDate.AddDays(skip);
            var adjustedEndDate = adjustedStartDate.AddDays(pageSize - 1);

            // Ensure we don't exceed the original end date
            if (adjustedEndDate > endDate)
            {
                adjustedEndDate = endDate;
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            var formattedStartDate = adjustedStartDate.ToString("yyyy-MM-dd");
            var formattedEndDate = adjustedEndDate.ToString("yyyy-MM-dd");

            var url = $"{formattedStartDate}..{formattedEndDate}?from={baseCurrency}";
            var response = await client.GetFromJsonAsync<CurrencyHistoricalRates>(url);

            if (response == null)
                throw new InvalidOperationException("Failed to retrieve historical currency rates from Frankfurter API");

            return (response, totalDays);
        }
    }
}