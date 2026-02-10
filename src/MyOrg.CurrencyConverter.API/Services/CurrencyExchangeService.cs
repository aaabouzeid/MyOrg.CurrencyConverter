using FluentValidation;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Core.Models.Requests;
using MyOrg.CurrencyConverter.API.Core.Models.Responses;

namespace MyOrg.CurrencyConverter.API.Services
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly ICurrencyProvider _currencyProvider;
        private readonly IValidator<GetLatestRatesRequest> _latestRatesValidator;
        private readonly IValidator<ConvertCurrencyRequest> _convertCurrencyValidator;
        private readonly IValidator<GetExchangeRateRequest> _exchangeRateValidator;
        private readonly IValidator<GetHistoricalRatesRequest> _historicalRatesValidator;

        public CurrencyExchangeService(
            ICurrencyProvider currencyProvider,
            IValidator<GetLatestRatesRequest> latestRatesValidator,
            IValidator<ConvertCurrencyRequest> convertCurrencyValidator,
            IValidator<GetExchangeRateRequest> exchangeRateValidator,
            IValidator<GetHistoricalRatesRequest> historicalRatesValidator)
        {
            _currencyProvider = currencyProvider ?? throw new ArgumentNullException(nameof(currencyProvider));
            _latestRatesValidator = latestRatesValidator ?? throw new ArgumentNullException(nameof(latestRatesValidator));
            _convertCurrencyValidator = convertCurrencyValidator ?? throw new ArgumentNullException(nameof(convertCurrencyValidator));
            _exchangeRateValidator = exchangeRateValidator ?? throw new ArgumentNullException(nameof(exchangeRateValidator));
            _historicalRatesValidator = historicalRatesValidator ?? throw new ArgumentNullException(nameof(historicalRatesValidator));
        }

        public async Task<CurrencyRates> GetLatestRatesAsync(GetLatestRatesRequest request)
        {
            await _latestRatesValidator.ValidateAndThrowAsync(request);
            return await _currencyProvider.GetLatestExchangeRates(request.BaseCurrency);
        }

        public async Task<decimal> ConvertCurrencyAsync(ConvertCurrencyRequest request)
        {
            await _convertCurrencyValidator.ValidateAndThrowAsync(request);

            var rateData = await _currencyProvider.GetExchangeRate(request.From, request.To);

            return rateData.ConvertAmount(request.Amount, request.To);
        }

        public async Task<CurrencyRates> GetExchangeRateAsync(GetExchangeRateRequest request)
        {
            await _exchangeRateValidator.ValidateAndThrowAsync(request);
            return await _currencyProvider.GetExchangeRate(request.From, request.To);
        }

        public async Task<PagedResult<PagedHistoricalRatesResponse>> GetHistoricalRatesAsync(GetHistoricalRatesRequest request)
        {
            await _historicalRatesValidator.ValidateAndThrowAsync(request);

            // Call provider with pagination parameters
            var (providerResult, totalDays) = await _currencyProvider.GetHistoricalExchangeRates(
                request.BaseCurrency,
                request.StartDate,
                request.EndDate,
                request.PageNumber,
                request.PageSize);

            // Build paginated response using factory method
            var pagedResponse = PagedHistoricalRatesResponse.FromCurrencyHistoricalRates(providerResult);

            // Create paged result with automatically calculated metadata
            return PagedResult<PagedHistoricalRatesResponse>.Create(
                pagedResponse,
                request.PageNumber,
                request.PageSize,
                totalDays);
        }
    }
}
