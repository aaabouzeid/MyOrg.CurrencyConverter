using FluentValidation;
using Polly;
using Polly.Extensions.Http;

namespace MyOrg.CurrencyConverter.API
{
    public class ServicesProviderHelper
    {
        public static IServiceCollection AddAppServices(IServiceCollection services, IConfiguration configuration)
        {
            // Infrastructure
            var frankfurterApiBaseUrl = configuration?.GetValue<string>("CurrencyProvider:FrankfurterApiBaseUrl") ?? throw new ArgumentNullException("CurrencyProvider:FrankfurterApiBaseUrl should be configured");

            services.AddHttpClient("FrankfurterApi", client =>
            {
                client.BaseAddress = new Uri(frankfurterApiBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy(configuration))
            .AddPolicyHandler(GetCircuitBreakerPolicy(configuration));

            services.AddTransient<Infrastructure.FrankfurterCurrencyProvider>();
            services.AddTransient<Core.Interfaces.ICurrencyProvider>(sp => sp.GetRequiredService<Infrastructure.FrankfurterCurrencyProvider>());

            // Read restricted currencies from configuration
            var restrictedCurrencies = configuration
                .GetSection("CurrencyRestrictions:RestrictedCurrencies")
                .Get<string[]>() ?? new[] { "TRY", "PLN", "THB", "MXN" };

            // Validators
            services.AddTransient<IValidator<Core.Models.Requests.GetLatestRatesRequest>, Core.Validators.GetLatestRatesRequestValidator>();
            services.AddTransient<IValidator<Core.Models.Requests.ConvertCurrencyRequest>>(sp =>
                new Core.Validators.ConvertCurrencyRequestValidator(restrictedCurrencies));
            services.AddTransient<IValidator<Core.Models.Requests.GetExchangeRateRequest>, Core.Validators.GetExchangeRateRequestValidator>();
            services.AddTransient<IValidator<Core.Models.Requests.GetHistoricalRatesRequest>, Core.Validators.GetHistoricalRatesRequestValidator>();

            // Application Services
            services.AddTransient<Services.ICurrencyExchangeService, Services.CurrencyExchangeService>();

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration configuration)
        {
            // Read retry configuration with defaults
            var retryCount = configuration.GetValue<int?>("ResiliencePolicies:RetryPolicy:RetryCount") ?? 3;

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: retryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s delay due to {outcome.Result?.StatusCode}");
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IConfiguration configuration)
        {
            // Read circuit breaker configuration with defaults
            var handledEventsAllowedBeforeBreaking = configuration.GetValue<int?>("ResiliencePolicies:CircuitBreakerPolicy:HandledEventsAllowedBeforeBreaking") ?? 5;
            var durationOfBreakInSeconds = configuration.GetValue<int?>("ResiliencePolicies:CircuitBreakerPolicy:DurationOfBreakInSeconds") ?? 30;

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (outcome, breakDelay) =>
                    {
                        Console.WriteLine($"Circuit breaker opened for {breakDelay.TotalSeconds}s due to {outcome.Result?.StatusCode}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit breaker reset");
                    });
        }
    }
}
