using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyOrg.CurrencyConverter.API.Data;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

namespace MyOrg.CurrencyConverter.API
{
    public class ServicesProviderHelper
    {
        public static IServiceCollection AddAppServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configure Database (PostgreSQL)
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' is required");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Configure Identity with Cookie and JWT Bearer authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
                options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
            })
            .AddBearerToken(IdentityConstants.BearerScheme);

            // Configure Identity with Roles support
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // User settings
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>() // Add role support
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints(); // Adds Identity API endpoints

            // Configure Authorization with policies
            services.AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
                .AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"))
                .AddPolicy("UserOrAbove", policy => policy.RequireRole("User", "Manager", "Admin"));

            // Configure JWT settings (keeping for reference, but Identity handles tokens)
            services.Configure<Core.Models.JwtSettings>(configuration.GetSection("JwtSettings"));

            // Configure provider settings
            services.Configure<Core.Models.CurrencyProviderSettings>(configuration.GetSection("CurrencyProviderSettings"));

            // Infrastructure - HTTP Clients for currency providers
            ConfigureHttpClients(services, configuration);

            // Register provider factory
            services.AddSingleton<Core.Interfaces.ICurrencyProviderFactory, Infrastructure.CurrencyProviderFactory>();

            // Configure cache settings
            services.Configure<Core.Models.CacheSettings>(configuration.GetSection("Cache"));

            // Conditional registration based on Cache:Enabled flag
            var cacheEnabled = configuration.GetValue<bool?>("Cache:Enabled") ?? false;

            if (cacheEnabled)
            {
                AddCachingServices(services, configuration);
            }
            else
            {
                // No caching - register provider directly from factory
                services.AddTransient<Core.Interfaces.ICurrencyProvider>(sp =>
                {
                    var factory = sp.GetRequiredService<Core.Interfaces.ICurrencyProviderFactory>();
                    return factory.CreateProvider();
                });
            }

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

        private static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
        {
            // Frankfurter API HttpClient
            var frankfurterApiBaseUrl = configuration.GetValue<string>("CurrencyProviderSettings:Frankfurter:BaseUrl")
                ?? "https://api.frankfurter.app";

            services.AddHttpClient("FrankfurterApi", client =>
            {
                client.BaseAddress = new Uri(frankfurterApiBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy(configuration))
            .AddPolicyHandler(GetCircuitBreakerPolicy(configuration));

            // Future providers' HttpClients can be added here:
            /*
            services.AddHttpClient("FixerApi", client =>
            {
                var baseUrl = configuration.GetValue<string>("CurrencyProviderSettings:Fixer:BaseUrl");
                client.BaseAddress = new Uri(baseUrl);
            })
            .AddPolicyHandler(GetRetryPolicy(configuration))
            .AddPolicyHandler(GetCircuitBreakerPolicy(configuration));
            */
        }

        private static void AddCachingServices(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("Cache:ConnectionString")
                ?? throw new InvalidOperationException("Cache:ConnectionString is required when caching is enabled");

            // Register Redis ConnectionMultiplexer as singleton (StackExchange.Redis best practice)
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false; // Allow app to start even if Redis is down
                options.ConnectTimeout = 5000;
                options.SyncTimeout = 5000;

                return ConnectionMultiplexer.Connect(options);
            });

            // Register cache service
            services.AddSingleton<Core.Interfaces.ICacheService, Infrastructure.RedisCacheService>();

            // Register decorated provider using factory
            services.AddTransient<Core.Interfaces.ICurrencyProvider>(sp =>
            {
                var factory = sp.GetRequiredService<Core.Interfaces.ICurrencyProviderFactory>();
                var innerProvider = factory.CreateProvider(); // Use factory to create provider
                var cacheService = sp.GetRequiredService<Core.Interfaces.ICacheService>();
                var cacheSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Core.Models.CacheSettings>>();
                var logger = sp.GetRequiredService<ILogger<Infrastructure.CachedCurrencyProvider>>();

                return new Infrastructure.CachedCurrencyProvider(innerProvider, cacheService, cacheSettings, logger);
            });
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
                        Serilog.Log.Warning("HTTP Retry {RetryCount} after {DelaySeconds:0.00}s delay due to {StatusCode}",
                            retryCount, timespan.TotalSeconds, outcome.Result?.StatusCode);
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
                        Serilog.Log.Error("Circuit breaker opened for {DurationSeconds:0.00}s due to {StatusCode}",
                            breakDelay.TotalSeconds, outcome.Result?.StatusCode);
                    },
                    onReset: () =>
                    {
                        Serilog.Log.Information("Circuit breaker reset");
                    });
        }
    }
}
