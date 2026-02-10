using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MyOrg.CurrencyConverter.API.Core.DTOs.Requests;
using MyOrg.CurrencyConverter.API.Core.DTOs.Responses;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.IntegrationTests.Infrastructure;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace MyOrg.CurrencyConverter.IntegrationTests.Controllers
{
    public class CurrencyControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CurrencyControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Add test authentication header
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer TestAuth");
        }

        #region GetLatestRates Tests

        [Fact]
        public async Task GetLatestRates_WithValidBaseCurrency_ReturnsOk()
        {
            // Arrange
            var baseCurrency = "USD";
            var expectedRates = new CurrencyRates
            {
                Base = baseCurrency,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m },
                    { "GBP", 0.73m },
                    { "JPY", 110.50m }
                }
            };

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetLatestRatesAsync(It.Is<GetLatestRatesRequest>(r => r.BaseCurrency == baseCurrency)))
                .ReturnsAsync(expectedRates);

            // Act
            var response = await _client.GetAsync($"/api/currency/latest/{baseCurrency}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<CurrencyRates>();
            result.Should().NotBeNull();
            result!.Base.Should().Be(baseCurrency);
            result.Rates.Should().ContainKey("EUR");
            result.Rates["EUR"].Should().Be(0.85m);
        }

        [Fact]
        public async Task GetLatestRates_WithInvalidBaseCurrency_ReturnsBadRequest()
        {
            // Arrange
            var invalidBaseCurrency = "INVALID";

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetLatestRatesAsync(It.Is<GetLatestRatesRequest>(r => r.BaseCurrency == invalidBaseCurrency)))
                .ThrowsAsync(new FluentValidation.ValidationException("Invalid currency code"));

            // Act
            var response = await _client.GetAsync($"/api/currency/latest/{invalidBaseCurrency}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetLatestRates_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var unauthenticatedClient = _factory.CreateClient();
            var baseCurrency = "USD";

            // Act
            var response = await unauthenticatedClient.GetAsync($"/api/currency/latest/{baseCurrency}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region ConvertCurrency Tests

        [Fact]
        public async Task ConvertCurrency_WithValidParameters_ReturnsConversionResult()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            var amount = 100m;
            var expectedRate = 0.85m;
            var expectedConvertedAmount = amount * expectedRate;

            var rateData = new CurrencyRates
            {
                Base = from,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, decimal> { { to, expectedRate } }
            };

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetExchangeRateAsync(It.Is<GetExchangeRateRequest>(r => r.From == from && r.To == to)))
                .ReturnsAsync(rateData);

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.ConvertCurrencyAsync(It.Is<ConvertCurrencyRequest>(r => r.From == from && r.To == to && r.Amount == amount)))
                .ReturnsAsync(expectedConvertedAmount);

            // Act
            var response = await _client.GetAsync($"/api/currency/convert?from={from}&to={to}&amount={amount}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ConversionResult>();
            result.Should().NotBeNull();
            result!.FromCurrency.Should().Be(from);
            result.ToCurrency.Should().Be(to);
            result.OriginalAmount.Should().Be(amount);
            result.ConvertedAmount.Should().Be(expectedConvertedAmount);
            result.ExchangeRate.Should().Be(expectedRate);
        }

        [Fact]
        public async Task ConvertCurrency_WithNegativeAmount_ReturnsBadRequest()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            var negativeAmount = -100m;

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.ConvertCurrencyAsync(It.Is<ConvertCurrencyRequest>(r => r.Amount == negativeAmount)))
                .ThrowsAsync(new FluentValidation.ValidationException("Amount must be positive"));

            // Act
            var response = await _client.GetAsync($"/api/currency/convert?from={from}&to={to}&amount={negativeAmount}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ConvertCurrency_WithZeroAmount_ReturnsBadRequest()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            var zeroAmount = 0m;

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.ConvertCurrencyAsync(It.Is<ConvertCurrencyRequest>(r => r.Amount == zeroAmount)))
                .ThrowsAsync(new FluentValidation.ValidationException("Amount must be greater than zero"));

            // Act
            var response = await _client.GetAsync($"/api/currency/convert?from={from}&to={to}&amount={zeroAmount}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ConvertCurrency_WithSameCurrency_ReturnsValidResult()
        {
            // Arrange
            var currency = "USD";
            var amount = 100m;

            var rateData = new CurrencyRates
            {
                Base = currency,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, decimal> { { currency, 1.0m } }
            };

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetExchangeRateAsync(It.Is<GetExchangeRateRequest>(r => r.From == currency && r.To == currency)))
                .ReturnsAsync(rateData);

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.ConvertCurrencyAsync(It.Is<ConvertCurrencyRequest>(r => r.From == currency && r.To == currency && r.Amount == amount)))
                .ReturnsAsync(amount);

            // Act
            var response = await _client.GetAsync($"/api/currency/convert?from={currency}&to={currency}&amount={amount}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ConversionResult>();
            result.Should().NotBeNull();
            result!.ConvertedAmount.Should().Be(amount);
            result.ExchangeRate.Should().Be(1.0m);
        }

        #endregion

        #region GetExchangeRate Tests

        [Fact]
        public async Task GetExchangeRate_WithValidCurrencies_ReturnsRateData()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            var expectedRate = 0.85m;

            var rateData = new CurrencyRates
            {
                Base = from,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, decimal> { { to, expectedRate } }
            };

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetExchangeRateAsync(It.Is<GetExchangeRateRequest>(r => r.From == from && r.To == to)))
                .ReturnsAsync(rateData);

            // Act
            var response = await _client.GetAsync($"/api/currency/rate?from={from}&to={to}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<CurrencyRates>();
            result.Should().NotBeNull();
            result!.Base.Should().Be(from);
            result.Rates.Should().ContainKey(to);
            result.Rates[to].Should().Be(expectedRate);
        }

        [Fact]
        public async Task GetExchangeRate_WithInvalidFromCurrency_ReturnsBadRequest()
        {
            // Arrange
            var invalidFrom = "INVALID";
            var to = "EUR";

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetExchangeRateAsync(It.Is<GetExchangeRateRequest>(r => r.From == invalidFrom)))
                .ThrowsAsync(new FluentValidation.ValidationException("Invalid from currency"));

            // Act
            var response = await _client.GetAsync($"/api/currency/rate?from={invalidFrom}&to={to}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetExchangeRate_WithInvalidToCurrency_ReturnsBadRequest()
        {
            // Arrange
            var from = "USD";
            var invalidTo = "INVALID";

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetExchangeRateAsync(It.Is<GetExchangeRateRequest>(r => r.To == invalidTo)))
                .ThrowsAsync(new FluentValidation.ValidationException("Invalid to currency"));

            // Act
            var response = await _client.GetAsync($"/api/currency/rate?from={from}&to={invalidTo}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetHistoricalRates Tests

        [Fact]
        public async Task GetHistoricalRates_WithValidParameters_ReturnsPagedResults()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var pageNumber = 1;
            var pageSize = 10;

            var historicalData = new PagedHistoricalRatesResponse
            {
                Base = baseCurrency,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2024-01-01", new Dictionary<string, decimal> { { "EUR", 0.85m } } },
                    { "2024-01-02", new Dictionary<string, decimal> { { "EUR", 0.86m } } }
                }
            };

            var pagination = new PaginationMetadata
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = 2,
                TotalPages = 1
            };

            var expectedResult = new PagedResult<PagedHistoricalRatesResponse>(historicalData, pagination);

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetHistoricalRatesAsync(It.Is<GetHistoricalRatesRequest>(r =>
                    r.BaseCurrency == baseCurrency &&
                    r.StartDate.Date == startDate.Date &&
                    r.EndDate.Date == endDate.Date &&
                    r.PageNumber == pageNumber &&
                    r.PageSize == pageSize)))
                .ReturnsAsync(expectedResult);

            // Act
            var response = await _client.GetAsync(
                $"/api/currency/historical?baseCurrency={baseCurrency}" +
                $"&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}" +
                $"&pageNumber={pageNumber}&pageSize={pageSize}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResult<PagedHistoricalRatesResponse>>();
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data.Rates.Should().NotBeEmpty();
            result.Pagination.CurrentPage.Should().Be(pageNumber);
            result.Pagination.PageSize.Should().Be(pageSize);
        }

        [Fact]
        public async Task GetHistoricalRates_WithEndDateBeforeStartDate_ReturnsBadRequest()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = new DateTime(2024, 1, 31);
            var endDate = new DateTime(2024, 1, 1);

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetHistoricalRatesAsync(It.IsAny<GetHistoricalRatesRequest>()))
                .ThrowsAsync(new FluentValidation.ValidationException("End date must be after start date"));

            // Act
            var response = await _client.GetAsync(
                $"/api/currency/historical?baseCurrency={baseCurrency}" +
                $"&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetHistoricalRates_WithInvalidPageNumber_ReturnsBadRequest()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var invalidPageNumber = 0;

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetHistoricalRatesAsync(It.IsAny<GetHistoricalRatesRequest>()))
                .ThrowsAsync(new FluentValidation.ValidationException("Page number must be greater than 0"));

            // Act
            var response = await _client.GetAsync(
                $"/api/currency/historical?baseCurrency={baseCurrency}" +
                $"&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}" +
                $"&pageNumber={invalidPageNumber}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetHistoricalRates_WithDefaultPagination_ReturnsFirstPage()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            var historicalData = new PagedHistoricalRatesResponse
            {
                Base = baseCurrency,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2024-01-01", new Dictionary<string, decimal> { { "EUR", 0.85m } } }
                }
            };

            var pagination = new PaginationMetadata
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 1,
                TotalPages = 1
            };

            var expectedResult = new PagedResult<PagedHistoricalRatesResponse>(historicalData, pagination);

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetHistoricalRatesAsync(It.Is<GetHistoricalRatesRequest>(r =>
                    r.PageNumber == 1 && r.PageSize == 10)))
                .ReturnsAsync(expectedResult);

            // Act - no pagination parameters, should use defaults (page 1, size 10)
            var response = await _client.GetAsync(
                $"/api/currency/historical?baseCurrency={baseCurrency}" +
                $"&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResult<PagedHistoricalRatesResponse>>();
            result.Should().NotBeNull();
            result!.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetLatestRates_WhenExternalApiDown_ReturnsServiceUnavailable()
        {
            // Arrange
            var baseCurrency = "USD";

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.GetLatestRatesAsync(It.IsAny<GetLatestRatesRequest>()))
                .ThrowsAsync(new HttpRequestException("External API is down"));

            // Act
            var response = await _client.GetAsync($"/api/currency/latest/{baseCurrency}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task ConvertCurrency_WhenExternalApiReturnsError_ReturnsBadGateway()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            var amount = 100m;

            _factory.MockCurrencyExchangeService?
                .Setup(s => s.ConvertCurrencyAsync(It.IsAny<ConvertCurrencyRequest>()))
                .ThrowsAsync(new InvalidOperationException("External API returned error"));

            // Act
            var response = await _client.GetAsync($"/api/currency/convert?from={from}&to={to}&amount={amount}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        #endregion
    }
}
