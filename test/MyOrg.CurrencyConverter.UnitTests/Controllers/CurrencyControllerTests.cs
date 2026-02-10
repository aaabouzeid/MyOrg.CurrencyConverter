using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyOrg.CurrencyConverter.API.Controllers;
using MyOrg.CurrencyConverter.API.Core.DTOs.Requests;
using MyOrg.CurrencyConverter.API.Core.DTOs.Responses;
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

            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<GetLatestRatesRequest>()))
                .ReturnsAsync(expectedRates);

            // Act
            var result = await _controller.GetLatestRates("USD");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedRates);
        }

        [Fact]
        public async Task GetLatestRates_ValidationException_ReturnsBadRequest()
        {
            // Arrange
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<GetLatestRatesRequest>()))
                .ThrowsAsync(new ValidationException("Validation failed"));

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
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<GetLatestRatesRequest>()))
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
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<GetLatestRatesRequest>()))
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
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<GetLatestRatesRequest>()))
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
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<ConvertCurrencyRequest>()))
                .ReturnsAsync(92m);

            _mockService.Setup(s => s.GetExchangeRateAsync(It.IsAny<GetExchangeRateRequest>()))
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
        public async Task ConvertCurrency_ValidationException_ReturnsBadRequest()
        {
            // Arrange
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<ConvertCurrencyRequest>()))
                .ThrowsAsync(new ValidationException("Invalid input"));

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
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<ConvertCurrencyRequest>()))
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
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<ConvertCurrencyRequest>()))
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

            _mockService.Setup(s => s.GetExchangeRateAsync(It.IsAny<GetExchangeRateRequest>()))
                .ReturnsAsync(expectedRate);

            // Act
            var result = await _controller.GetExchangeRate("USD", "EUR");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedRate);
        }

        [Fact]
        public async Task GetExchangeRate_ValidationException_ReturnsBadRequest()
        {
            // Arrange
            _mockService.Setup(s => s.GetExchangeRateAsync(It.IsAny<GetExchangeRateRequest>()))
                .ThrowsAsync(new ValidationException("Invalid currency"));

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
            _mockService.Setup(s => s.GetExchangeRateAsync(It.IsAny<GetExchangeRateRequest>()))
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
        public async Task GetHistoricalRates_ValidInputsWithDefaultPagination_ReturnsOkWithPagedResults()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var pagedResponse = new PagedHistoricalRatesResponse
            {
                Base = "USD",
                StartDate = "2024-01-01",
                EndDate = "2024-01-31",
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };
            var paginationMetadata = new PaginationMetadata
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 31,
                TotalPages = 4
            };
            var expectedResult = new PagedResult<PagedHistoricalRatesResponse>(pagedResponse, paginationMetadata);

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<GetHistoricalRatesRequest>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetHistoricalRates("USD", startDate, endDate);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var pagedResult = okResult.Value.Should().BeOfType<PagedResult<PagedHistoricalRatesResponse>>().Subject;
            pagedResult.Data.Base.Should().Be("USD");
            pagedResult.Pagination.CurrentPage.Should().Be(1);
            pagedResult.Pagination.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetHistoricalRates_WithCustomPagination_PassesParametersCorrectly()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 12, 31);
            var pagedResponse = new PagedHistoricalRatesResponse
            {
                Base = "USD",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                Rates = new Dictionary<string, Dictionary<string, decimal>>()
            };
            var paginationMetadata = new PaginationMetadata
            {
                CurrentPage = 3,
                PageSize = 20,
                TotalCount = 366,
                TotalPages = 19
            };
            var expectedResult = new PagedResult<PagedHistoricalRatesResponse>(pagedResponse, paginationMetadata);

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.Is<GetHistoricalRatesRequest>(r =>
                r.PageNumber == 3 && r.PageSize == 20)))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetHistoricalRates("USD", startDate, endDate, 3, 20);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var pagedResult = okResult.Value.Should().BeOfType<PagedResult<PagedHistoricalRatesResponse>>().Subject;
            pagedResult.Pagination.CurrentPage.Should().Be(3);
            pagedResult.Pagination.PageSize.Should().Be(20);
        }

        [Fact]
        public async Task GetHistoricalRates_ValidationException_ReturnsBadRequest()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<GetHistoricalRatesRequest>()))
                .ThrowsAsync(new ValidationException("Invalid date range"));

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

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<GetHistoricalRatesRequest>()))
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

            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<GetHistoricalRatesRequest>()))
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
