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
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            services.AddTransient<Infrastructure.FrankfurterCurrencyProvider>();
            services.AddTransient<Core.Interfaces.ICurrencyProvider>(sp => sp.GetRequiredService<Infrastructure.FrankfurterCurrencyProvider>());

            // Application Services
            services.AddTransient<Services.ICurrencyExchangeService, Services.CurrencyExchangeService>();

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s delay due to {outcome.Result?.StatusCode}");
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
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
