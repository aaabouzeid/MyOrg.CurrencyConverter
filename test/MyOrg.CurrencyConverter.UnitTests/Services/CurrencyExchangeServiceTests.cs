using FluentAssertions;
using Moq;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Services;

namespace MyOrg.CurrencyConverter.UnitTests.Services
{
    public class CurrencyExchangeServiceTests
    {
        private readonly Mock<ICurrencyProvider> _mockProvider;
        private readonly CurrencyExchangeService _service;

        public CurrencyExchangeServiceTests()
        {
            _mockProvider = new Mock<ICurrencyProvider>();
            _service = new CurrencyExchangeService(_mockProvider.Object);
        }

        [Fact]
        public void Constructor_NullProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new CurrencyExchangeService(null!);
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

            // Act
            var result = await _service.GetLatestRatesAsync("USD");

            // Assert
            result.Should().Be(expectedRates);
            _mockProvider.Verify(p => p.GetLatestExchangeRates("USD"), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetLatestRatesAsync_InvalidCurrency_ThrowsArgumentException(string? currency)
        {
            // Act & Assert
            var action = async () => await _service.GetLatestRatesAsync(currency!);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("baseCurrency");
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

            // Act
            var result = await _service.ConvertCurrencyAsync("USD", "EUR", 100m);

            // Assert
            result.Should().Be(92m);
            _mockProvider.Verify(p => p.GetExchangeRate("USD", "EUR"), Times.Once);
        }

        [Theory]
        [InlineData(null, "EUR")]
        [InlineData("", "EUR")]
        [InlineData("   ", "EUR")]
        public async Task ConvertCurrencyAsync_InvalidFromCurrency_ThrowsArgumentException(string? from, string to)
        {
            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(from!, to, 100m);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("from");
        }

        [Theory]
        [InlineData("USD", null)]
        [InlineData("USD", "")]
        [InlineData("USD", "   ")]
        public async Task ConvertCurrencyAsync_InvalidToCurrency_ThrowsArgumentException(string from, string? to)
        {
            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync(from, to!, 100m);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("to");
        }

        [Fact]
        public async Task ConvertCurrencyAsync_NegativeAmount_ThrowsArgumentException()
        {
            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync("USD", "EUR", -100m);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("amount");
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

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync("USD", "EUR", 100m);
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

            // Act & Assert
            var action = async () => await _service.ConvertCurrencyAsync("USD", "EUR", 100m);
            await action.Should().ThrowAsync<InvalidOperationException>();
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

            // Act
            var result = await _service.GetExchangeRateAsync("USD", "EUR");

            // Assert
            result.Should().Be(expectedRate);
            _mockProvider.Verify(p => p.GetExchangeRate("USD", "EUR"), Times.Once);
        }

        [Theory]
        [InlineData(null, "EUR")]
        [InlineData("", "EUR")]
        [InlineData("   ", "EUR")]
        public async Task GetExchangeRateAsync_InvalidFromCurrency_ThrowsArgumentException(string? from, string to)
        {
            // Act & Assert
            var action = async () => await _service.GetExchangeRateAsync(from!, to);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("from");
        }

        [Theory]
        [InlineData("USD", null)]
        [InlineData("USD", "")]
        [InlineData("USD", "   ")]
        public async Task GetExchangeRateAsync_InvalidToCurrency_ThrowsArgumentException(string from, string? to)
        {
            // Act & Assert
            var action = async () => await _service.GetExchangeRateAsync(from, to!);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("to");
        }

        #endregion

        #region GetHistoricalRatesAsync Tests

        [Fact]
        public async Task GetHistoricalRatesAsync_ValidInputs_ReturnsHistoricalRates()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var expectedRates = new CurrencyHistoricalRates
            {
                Base = "USD",
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };

            _mockProvider.Setup(p => p.GetHistoricalExchangeRates("USD", startDate, endDate))
                .ReturnsAsync(expectedRates);

            // Act
            var result = await _service.GetHistoricalRatesAsync("USD", startDate, endDate);

            // Assert
            result.Should().Be(expectedRates);
            _mockProvider.Verify(p => p.GetHistoricalExchangeRates("USD", startDate, endDate), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetHistoricalRatesAsync_InvalidCurrency_ThrowsArgumentException(string? currency)
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync(currency!, startDate, endDate);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("baseCurrency");
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_StartDateAfterEndDate_ThrowsArgumentException()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 31);
            var endDate = new DateTime(2024, 1, 1);

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync("USD", startDate, endDate);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("startDate");
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_StartDateEqualsEndDate_ThrowsArgumentException()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1);

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync("USD", date, date);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("startDate");
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_EndDateInFuture_ThrowsArgumentException()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date.AddDays(-10);
            var endDate = DateTime.UtcNow.Date.AddDays(1);

            // Act & Assert
            var action = async () => await _service.GetHistoricalRatesAsync("USD", startDate, endDate);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("endDate");
        }

        #endregion
    }
}
