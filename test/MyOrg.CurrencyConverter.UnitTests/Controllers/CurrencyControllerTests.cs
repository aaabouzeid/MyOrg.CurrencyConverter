using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyOrg.CurrencyConverter.API.Controllers;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Services;

namespace MyOrg.CurrencyConverter.UnitTests.Controllers
{
    public class CurrencyControllerTests
    {
        private readonly Mock<ICurrencyExchangeService> _mockService;
        private readonly Mock<ILogger<CurrencyController>> _mockLogger;
        private readonly CurrencyController _controller;

        public CurrencyControllerTests()
        {
            _mockService = new Mock<ICurrencyExchangeService>();
            _mockLogger = new Mock<ILogger<CurrencyController>>();
            _controller = new CurrencyController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new CurrencyController(null!, _mockLogger.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("exchangeService");
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new CurrencyController(_mockService.Object, null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        #region GetLatestRates Tests

        [Fact]
        public async Task GetLatestRates_ValidCurrency_ReturnsOkWithRates()
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

            _mockService.Setup(s => s.GetLatestRatesAsync("USD"))
                .ReturnsAsync(expectedRates);

            // Act
            var result = await _controller.GetLatestRates("USD");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedRates);
            _mockService.Verify(s => s.GetLatestRatesAsync("USD"), Times.Once);
        }

        [Fact]
        public async Task GetLatestRates_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid currency"));

            // Act
            var result = await _controller.GetLatestRates("INVALID");

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task GetLatestRates_InvalidOperationException_Returns502BadGateway()
        {
            // Arrange
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("External API error"));

            // Act
            var result = await _controller.GetLatestRates("USD");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        }

        [Fact]
        public async Task GetLatestRates_HttpRequestException_Returns503ServiceUnavailable()
        {
            // Arrange
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Service unavailable"));

            // Act
            var result = await _controller.GetLatestRates("USD");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        }

        [Fact]
        public async Task GetLatestRates_UnexpectedException_Returns500InternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetLatestRates("USD");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region ConvertCurrency Tests

        [Fact]
        public async Task ConvertCurrency_ValidInputs_ReturnsOkWithConversionResult()
        {
            // Arrange
            _mockService.Setup(s => s.ConvertCurrencyAsync("USD", "EUR", 100m))
                .ReturnsAsync(92m);

            _mockService.Setup(s => s.GetExchangeRateAsync("USD", "EUR"))
                .ReturnsAsync(new CurrencyRates
                {
                    Base = "USD",
                    Date = "2024-01-01",
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
                });

            // Act
            var result = await _controller.ConvertCurrency("USD", "EUR", 100m);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var conversionResult = okResult.Value.Should().BeOfType<ConversionResult>().Subject;
            conversionResult.FromCurrency.Should().Be("USD");
            conversionResult.ToCurrency.Should().Be("EUR");
            conversionResult.OriginalAmount.Should().Be(100m);
            conversionResult.ConvertedAmount.Should().Be(92m);
            conversionResult.ExchangeRate.Should().Be(0.92m);
        }

        [Fact]
        public async Task ConvertCurrency_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ThrowsAsync(new ArgumentException("Invalid input"));

            // Act
            var result = await _controller.ConvertCurrency("", "EUR", 100m);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ConvertCurrency_InvalidOperationException_Returns502BadGateway()
        {
            // Arrange
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ThrowsAsync(new InvalidOperationException("External API error"));

            // Act
            var result = await _controller.ConvertCurrency("USD", "EUR", 100m);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        }

        [Fact]
        public async Task ConvertCurrency_HttpRequestException_Returns503ServiceUnavailable()
        {
            // Arrange
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ThrowsAsync(new HttpRequestException("Service unavailable"));

            // Act
            var result = await _controller.ConvertCurrency("USD", "EUR", 100m);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        }

        #endregion

        #region GetExchangeRate Tests

        [Fact]
        public async Task GetExchangeRate_ValidCurrencies_ReturnsOkWithRate()
        {
            // Arrange
            var expectedRate = new CurrencyRates
            {
                Base = "USD",
                Date = "2024-01-01",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
            };

            _mockService.Setup(s => s.GetExchangeRateAsync("USD", "EUR"))
                .ReturnsAsync(expectedRate);

            // Act
            var result = await _controller.GetExchangeRate("USD", "EUR");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedRate);
            _mockService.Verify(s => s.GetExchangeRateAsync("USD", "EUR"), Times.Once);
        }

        [Fact]
        public async Task GetExchangeRate_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            _mockService.Setup(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid currency"));

            // Act
            var result = await _controller.GetExchangeRate("", "EUR");

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task GetExchangeRate_InvalidOperationException_Returns502BadGateway()
        {
            // Arrange
            _mockService.Setup(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("External API error"));

            // Act
            var result = await _controller.GetExchangeRate("USD", "EUR");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        }

        #endregion

        #region GetHistoricalRates Tests

        [Fact]
        public async Task GetHistoricalRates_ValidInputs_ReturnsOkWithHistoricalRates()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var expectedRates = new CurrencyHistoricalRates
            {
                Base = "USD",
                StartDate = "2024-01-01",
                EndDate = "2024-01-31",
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };

            _mockService.Setup(s => s.GetHistoricalRatesAsync("USD", startDate, endDate))
                .ReturnsAsync(expectedRates);

            // Act
            var result = await _controller.GetHistoricalRates("USD", startDate, endDate);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedRates);
            _mockService.Verify(s => s.GetHistoricalRatesAsync("USD", startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetHistoricalRates_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new ArgumentException("Invalid date range"));

            // Act
            var result = await _controller.GetHistoricalRates("", startDate, endDate);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task GetHistoricalRates_InvalidOperationException_Returns502BadGateway()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("External API error"));

            // Act
            var result = await _controller.GetHistoricalRates("USD", startDate, endDate);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        }

        [Fact]
        public async Task GetHistoricalRates_HttpRequestException_Returns503ServiceUnavailable()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new HttpRequestException("Service unavailable"));

            // Act
            var result = await _controller.GetHistoricalRates("USD", startDate, endDate);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        }

        #endregion
    }
}
