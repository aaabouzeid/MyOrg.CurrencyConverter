using FluentValidation;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Core.Models.Requests;

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

            if (rateData?.Rates == null || !rateData.Rates.ContainsKey(request.To))
                throw new InvalidOperationException($"Exchange rate for {request.To} not found");

            var rate = rateData.Rates[request.To];
            return request.Amount * rate;
        }

        public async Task<CurrencyRates> GetExchangeRateAsync(GetExchangeRateRequest request)
        {
            await _exchangeRateValidator.ValidateAndThrowAsync(request);
            return await _currencyProvider.GetExchangeRate(request.From, request.To);
        }

        public async Task<CurrencyHistoricalRates> GetHistoricalRatesAsync(GetHistoricalRatesRequest request)
        {
            await _historicalRatesValidator.ValidateAndThrowAsync(request);
            return await _currencyProvider.GetHistoricalExchangeRates(request.BaseCurrency, request.StartDate, request.EndDate);
        }
    }
}
