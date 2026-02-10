using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Core.Models.Requests;
using MyOrg.CurrencyConverter.API.Core.Models.Responses;
using MyOrg.CurrencyConverter.API.Services;

namespace MyOrg.CurrencyConverter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyExchangeService _exchangeService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(
            ICurrencyExchangeService exchangeService,
            ILogger<CurrencyController> logger)
        {
            _exchangeService = exchangeService ?? throw new ArgumentNullException(nameof(exchangeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all latest exchange rates for a base currency
        /// </summary>
        /// <param name="baseCurrency">The base currency code (e.g., USD, EUR)</param>
        /// <returns>All exchange rates for the base currency</returns>
        [HttpGet("latest/{baseCurrency}")]
        [ProducesResponseType(typeof(CurrencyRates), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetLatestRates(string baseCurrency)
        {
            try
            {
                _logger.LogInformation("Getting latest rates for base currency: {BaseCurrency}", baseCurrency);

                var request = new GetLatestRatesRequest { BaseCurrency = baseCurrency };
                var rates = await _exchangeService.GetLatestRatesAsync(request);

                return Ok(rates);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for GetLatestRates: {Errors}", ex.Errors);
                return BadRequest(new { errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in GetLatestRates: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "External API error", details = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed in GetLatestRates: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetLatestRates: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Convert an amount from one currency to another
        /// </summary>
        /// <param name="from">Source currency code</param>
        /// <param name="to">Target currency code</param>
        /// <param name="amount">Amount to convert</param>
        /// <returns>Conversion result with exchange rate and converted amount</returns>
        [HttpGet("convert")]
        [ProducesResponseType(typeof(ConversionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> ConvertCurrency(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] decimal amount)
        {
            try
            {
                _logger.LogInformation("Converting {Amount} {From} to {To}", amount, from, to);

                var convertRequest = new ConvertCurrencyRequest { From = from, To = to, Amount = amount };
                var convertedAmount = await _exchangeService.ConvertCurrencyAsync(convertRequest);

                var rateRequest = new GetExchangeRateRequest { From = from, To = to };
                var rateData = await _exchangeService.GetExchangeRateAsync(rateRequest);

                var result = new ConversionResult
                {
                    FromCurrency = from.ToUpperInvariant(),
                    ToCurrency = to.ToUpperInvariant(),
                    OriginalAmount = amount,
                    ConvertedAmount = convertedAmount,
                    ExchangeRate = rateData.Rates.ContainsKey(to.ToUpperInvariant())
                        ? rateData.Rates[to.ToUpperInvariant()]
                        : (convertedAmount / amount),
                    Date = rateData.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd")
                };

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for ConvertCurrency: {Errors}", ex.Errors);
                return BadRequest(new { errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in ConvertCurrency: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "External API error", details = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed in ConvertCurrency: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ConvertCurrency: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get the exchange rate between two currencies
        /// </summary>
        /// <param name="from">Source currency code</param>
        /// <param name="to">Target currency code</param>
        /// <returns>Exchange rate information</returns>
        [HttpGet("rate")]
        [ProducesResponseType(typeof(CurrencyRates), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetExchangeRate(
            [FromQuery] string from,
            [FromQuery] string to)
        {
            try
            {
                _logger.LogInformation("Getting exchange rate from {From} to {To}", from, to);

                var request = new GetExchangeRateRequest { From = from, To = to };
                var rate = await _exchangeService.GetExchangeRateAsync(request);

                return Ok(rate);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for GetExchangeRate: {Errors}", ex.Errors);
                return BadRequest(new { errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in GetExchangeRate: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "External API error", details = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed in GetExchangeRate: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetExchangeRate: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get historical exchange rates for a base currency over a date range with pagination
        /// </summary>
        /// <param name="baseCurrency">The base currency code</param>
        /// <param name="startDate">Start date (YYYY-MM-DD)</param>
        /// <param name="endDate">End date (YYYY-MM-DD)</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <returns>Paginated historical exchange rates</returns>
        [HttpGet("historical")]
        [ProducesResponseType(typeof(PagedResult<PagedHistoricalRatesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetHistoricalRates(
            [FromQuery] string baseCurrency,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation(
                    "Getting historical rates for {BaseCurrency} from {StartDate} to {EndDate} (Page: {PageNumber}, Size: {PageSize})",
                    baseCurrency, startDate, endDate, pageNumber, pageSize);

                var request = new GetHistoricalRatesRequest
                {
                    BaseCurrency = baseCurrency,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var rates = await _exchangeService.GetHistoricalRatesAsync(request);

                return Ok(rates);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for GetHistoricalRates: {Errors}", ex.Errors);
                return BadRequest(new { errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in GetHistoricalRates: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "External API error", details = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed in GetHistoricalRates: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetHistoricalRates: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }
    }
}
