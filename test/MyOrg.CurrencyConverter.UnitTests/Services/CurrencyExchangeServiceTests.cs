using FluentAssertions;
using FluentValidation;
using Moq;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Core.Models.Requests;
using MyOrg.CurrencyConverter.API.Core.Models.Responses;
using MyOrg.CurrencyConverter.API.Core.Validators;
using MyOrg.CurrencyConverter.API.Services;

namespace MyOrg.CurrencyConverter.UnitTests.Services
{
    public class CurrencyExchangeServiceTests
    {
        private readonly Mock<ICurrencyProvider> _mockProvider;
        private readonly CurrencyExchangeService _service;
        private readonly string[] _restrictedCurrencies = new[] { "TRY", "PLN", "THB", "MXN" };

        public CurrencyExchangeServiceTests()
        {
            _mockProvider = new Mock<ICurrencyProvider>();

            _service = new CurrencyExchangeService(
                _mockProvider.Object,
                new GetLatestRatesRequestValidator(),
                new ConvertCurrencyRequestValidator(_restrictedCurrencies),
                new GetExchangeRateRequestValidator(),
                new GetHistoricalRatesRequestValidator());
        }

        [Fact]
        public void Constructor_NullProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new CurrencyExchangeService(
                null!,
                new GetLatestRatesRequestValidator(),
                new ConvertCurrencyRequestValidator(_restrictedCurrencies),
                new GetExchangeRateRequestValidator(),
                new GetHistoricalRatesRequestValidator());

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("currencyProvider");
        }

        #region GetLatestRatesAsync Tests

        [Fact]
        public async Task GetLatestRatesAsync_ValidCurrency_ReturnsRates()
        {
            // Arrange
            var expectedRates = new CurrencyRates
            {
                Base = "USD",
                Date = "2024-01-01",
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.92m },
                    { "GBP", 0.79m }
                }
            };

            _mockProvider.Setup(p => p.GetLatestExchangeRates("USD"))
                .ReturnsAsync(expectedRates);

            var request = new GetLatestRatesRequest { BaseCurrency = "USD" };

            // Act
            var result = await _service.GetLatestRatesAsync(request);

            // Assert
            result.Should().Be(expectedRates);
            _mockProvider.Verify(p => p.GetLatestExchangeRates("USD"), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetLatestRatesAsync_InvalidCurrency_ThrowsValidationException(string? currency)
        {
            // Arrange
            var request = new GetLatestRatesRequest { BaseCurrency = currency! };

            // Act & Assert
            var action = async () => await _service.GetLatestRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetLatestRatesAsync_InvalidCurrencyFormat_ThrowsValidationException()
        {
            // Arrange
            var request = new GetLatestRatesRequest { BaseCurrency = "US" }; // Only 2 characters

            // Act & Assert
            var action = async () => await _service.GetLatestRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        #endregion

        #region ConvertCurrencyAsync Tests

        [Fact]
        public async Task ConvertCurrencyAsync_ValidInputs_ReturnsCorrectAmount()
        {
            // Arrange
            _mockProvider.Setup(p => p.GetExchangeRate("USD", "EUR"))
                .ReturnsAsync(new CurrencyRates
                {
                    Base = "USD",
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
                });

            var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = 100m };

            // Act
            var result = await _service.ConvertCurrencyAsync(request);

            // Assert
            result.Should().Be(92m);
            _mockProvider.Verify(p => p.GetExchangeRate("USD", "EUR"), Times.Once);
        }

        [Theory]
        [InlineData(null, "EUR")]
        [InlineData("", "EUR")]
        [InlineData("   ", "EUR")]
        public async Task ConvertCurrencyAsync_InvalidFromCurrency_ThrowsValidationException(string? from, string to)
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = from!, To = to, Amount = 100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Theory]
        [InlineData("USD", null)]
        [InlineData("USD", "")]
        [InlineData("USD", "   ")]
        public async Task ConvertCurrencyAsync_InvalidToCurrency_ThrowsValidationException(string from, string? to)
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = from, To = to!, Amount = 100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task ConvertCurrencyAsync_NegativeAmount_ThrowsValidationException()
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = -100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task ConvertCurrencyAsync_MissingRate_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockProvider.Setup(p => p.GetExchangeRate("USD", "EUR"))
                .ReturnsAsync(new CurrencyRates
                {
                    Base = "USD",
                    Rates = new Dictionary<string, decimal>()
                });

            var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = 100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Exchange rate for EUR not found");
        }

        [Fact]
        public async Task ConvertCurrencyAsync_NullRates_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockProvider.Setup(p => p.GetExchangeRate("USD", "EUR"))
                .ReturnsAsync(new CurrencyRates
                {
                    Base = "USD",
                    Rates = null!
                });

            var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = 100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Theory]
        [InlineData("TRY")]
        [InlineData("PLN")]
        [InlineData("THB")]
        [InlineData("MXN")]
        public async Task ConvertCurrencyAsync_RestrictedFromCurrency_ThrowsValidationException(string restrictedCurrency)
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = restrictedCurrency, To = "USD", Amount = 100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            var exception = await action.Should().ThrowAsync<ValidationException>();
            exception.Which.Errors.Should().Contain(e =>
                e.PropertyName == "From" &&
                e.ErrorMessage.Contains("not supported") &&
                e.ErrorMessage.Contains(restrictedCurrency));
        }

        [Theory]
        [InlineData("TRY")]
        [InlineData("PLN")]
        [InlineData("THB")]
        [InlineData("MXN")]
        public async Task ConvertCurrencyAsync_RestrictedToCurrency_ThrowsValidationException(string restrictedCurrency)
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "USD", To = restrictedCurrency, Amount = 100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            var exception = await action.Should().ThrowAsync<ValidationException>();
            exception.Which.Errors.Should().Contain(e =>
                e.PropertyName == "To" &&
                e.ErrorMessage.Contains("not supported") &&
                e.ErrorMessage.Contains(restrictedCurrency));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_BothCurrenciesRestricted_ThrowsValidationException()
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "TRY", To = "PLN", Amount = 100m };

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(request);
            var exception = await action.Should().ThrowAsync<ValidationException>();
            exception.Which.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        #endregion

        #region GetExchangeRateAsync Tests

        [Fact]
        public async Task GetExchangeRateAsync_ValidCurrencies_ReturnsRate()
        {
            // Arrange
            var expectedRate = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
            };

            _mockProvider.Setup(p => p.GetExchangeRate("USD", "EUR"))
                .ReturnsAsync(expectedRate);

            var request = new GetExchangeRateRequest { From = "USD", To = "EUR" };

            // Act
            var result = await _service.GetExchangeRateAsync(request);

            // Assert
            result.Should().Be(expectedRate);
            _mockProvider.Verify(p => p.GetExchangeRate("USD", "EUR"), Times.Once);
        }

        [Theory]
        [InlineData(null, "EUR")]
        [InlineData("", "EUR")]
        [InlineData("   ", "EUR")]
        public async Task GetExchangeRateAsync_InvalidFromCurrency_ThrowsValidationException(string? from, string to)
        {
            // Arrange
            var request = new GetExchangeRateRequest { From = from!, To = to };

            // Act & Assert
            var action = async () => await _service.GetExchangeRateAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Theory]
        [InlineData("USD", null)]
        [InlineData("USD", "")]
        [InlineData("USD", "   ")]
        public async Task GetExchangeRateAsync_InvalidToCurrency_ThrowsValidationException(string from, string? to)
        {
            // Arrange
            var request = new GetExchangeRateRequest { From = from, To = to! };

            // Act & Assert
            var action = async () => await _service.GetExchangeRateAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        #endregion

        #region GetHistoricalRatesAsync Tests

        [Fact]
        public async Task GetHistoricalRatesAsync_ValidInputsWithDefaultPagination_ReturnsPagedResults()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var expectedRates = new CurrencyHistoricalRates
            {
                Base = "USD",
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2024-01-01", new Dictionary<string, decimal> { { "EUR", 0.92m } } },
                    { "2024-01-02", new Dictionary<string, decimal> { { "EUR", 0.93m } } }
                }
            };

            _mockProvider.Setup(p => p.GetHistoricalExchangeRates("USD", startDate, endDate, 1, 10))
                .ReturnsAsync((expectedRates, 31));

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetHistoricalRatesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Base.Should().Be("USD");
            result.Data.Rates.Should().HaveCount(2);
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);
            result.Pagination.TotalCount.Should().Be(31);
            result.Pagination.TotalPages.Should().Be(4);
            result.Pagination.HasNext.Should().BeTrue();
            result.Pagination.HasPrevious.Should().BeFalse();
            _mockProvider.Verify(p => p.GetHistoricalExchangeRates("USD", startDate, endDate, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_CustomPagination_ReturnsCorrectPage()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 12, 31);
            var expectedRates = new CurrencyHistoricalRates
            {
                Base = "USD",
                StartDate = "2024-01-21",
                EndDate = "2024-01-40",
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };

            _mockProvider.Setup(p => p.GetHistoricalExchangeRates("USD", startDate, endDate, 3, 20))
                .ReturnsAsync((expectedRates, 366));

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = 3,
                PageSize = 20
            };

            // Act
            var result = await _service.GetHistoricalRatesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.CurrentPage.Should().Be(3);
            result.Pagination.PageSize.Should().Be(20);
            result.Pagination.TotalCount.Should().Be(366);
            result.Pagination.TotalPages.Should().Be(19);
            result.Pagination.HasNext.Should().BeTrue();
            result.Pagination.HasPrevious.Should().BeTrue();
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_LastPage_HasNextShouldBeFalse()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var expectedRates = new CurrencyHistoricalRates
            {
                Base = "USD",
                StartDate = "2024-01-31",
                EndDate = "2024-01-31",
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };

            _mockProvider.Setup(p => p.GetHistoricalExchangeRates("USD", startDate, endDate, 4, 10))
                .ReturnsAsync((expectedRates, 31));

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = 4,
                PageSize = 10
            };

            // Act
            var result = await _service.GetHistoricalRatesAsync(request);

            // Assert
            result.Pagination.CurrentPage.Should().Be(4);
            result.Pagination.TotalPages.Should().Be(4);
            result.Pagination.HasNext.Should().BeFalse();
            result.Pagination.HasPrevious.Should().BeTrue();
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_SinglePage_HasNextAndPreviousShouldBeFalse()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 5);
            var expectedRates = new CurrencyHistoricalRates
            {
                Base = "USD",
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };

            _mockProvider.Setup(p => p.GetHistoricalExchangeRates("USD", startDate, endDate, 1, 10))
                .ReturnsAsync((expectedRates, 5));

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetHistoricalRatesAsync(request);

            // Assert
            result.Pagination.TotalPages.Should().Be(1);
            result.Pagination.HasNext.Should().BeFalse();
            result.Pagination.HasPrevious.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetHistoricalRatesAsync_InvalidCurrency_ThrowsValidationException(string? currency)
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = currency!,
                StartDate = startDate,
                EndDate = endDate
            };

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_StartDateAfterEndDate_ThrowsValidationException()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 31);
            var endDate = new DateTime(2024, 1, 1);

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate
            };

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_StartDateEqualsEndDate_ThrowsValidationException()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1);

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = date,
                EndDate = date
            };

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_EndDateInFuture_ThrowsValidationException()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date.AddDays(-10);
            var endDate = DateTime.UtcNow.Date.AddDays(1);

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate
            };

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task GetHistoricalRatesAsync_InvalidPageNumber_ThrowsValidationException(int pageNumber)
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = pageNumber,
                PageSize = 10
            };

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task GetHistoricalRatesAsync_InvalidPageSize_ThrowsValidationException(int pageSize)
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = 1,
                PageSize = pageSize
            };

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync(request);
            await action.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_LargePageSize_IsValid()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 12, 31);
            var expectedRates = new CurrencyHistoricalRates
            {
                Base = "USD",
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };

            _mockProvider.Setup(p => p.GetHistoricalExchangeRates("USD", startDate, endDate, 1, 500))
                .ReturnsAsync((expectedRates, 366));

            var request = new GetHistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = 1,
                PageSize = 500
            };

            // Act
            var result = await _service.GetHistoricalRatesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.PageSize.Should().Be(500);
            result.Pagination.TotalPages.Should().Be(1);
        }

        #endregion
    }
}
