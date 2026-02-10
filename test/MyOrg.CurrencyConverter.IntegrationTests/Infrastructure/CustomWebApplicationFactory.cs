using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MyOrg.CurrencyConverter.API.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using MyOrg.CurrencyConverter.IntegrationTests.Infrastructure;
using MyOrg.CurrencyConverter.API.Services;
using Moq;

namespace MyOrg.CurrencyConverter.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<MyOrg.CurrencyConverter.API.Program>
    {
        public Mock<ICurrencyExchangeService>? MockCurrencyExchangeService { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Set environment to Testing - this is the key for integration tests
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test-specific configuration (these override the default appsettings.json)
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "InMemoryDatabase", // Use in-memory for tests
                    ["Cache:Enabled"] = "false", // Disable Redis cache for tests
                    ["RateLimiting:Enabled"] = "false", // Disable rate limiting for tests
                    ["OpenTelemetry:Enabled"] = "false", // Disable OpenTelemetry for tests
                    ["AutoMigrate"] = "false", // Disable auto-migration for tests
                    ["SeedDefaultAdmin"] = "false", // Disable admin seeding for tests
                    ["Serilog:MinimumLevel:Default"] = "Fatal", // Suppress Serilog output in tests
                    ["JwtSettings:SecretKey"] = "TestSecretKeyForJwtTokenGeneration!MustBeAtLeast32Characters",
                    ["JwtSettings:Issuer"] = "CurrencyConverter.API.Test",
                    ["JwtSettings:Audience"] = "CurrencyConverter.Clients.Test",
                    ["JwtSettings:ExpirationMinutes"] = "60"
                });
            });

            // Use ConfigureTestServices to override services AFTER the application's ConfigureServices has run
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL DbContext-related descriptors to avoid provider conflicts
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType.FullName != null &&
                               (d.ServiceType.FullName.Contains("DbContext") ||
                                d.ServiceType.FullName.Contains("EntityFrameworkCore")))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add fresh in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestDb");
                }, ServiceLifetime.Scoped);

                // Remove existing authentication services
                services.RemoveAll(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService));
                services.RemoveAll(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider));
                services.RemoveAll(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider));

                // Add test authentication scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                    options.DefaultScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

                // Mock the currency exchange service
                // This allows tests to control the responses from the service layer
                MockCurrencyExchangeService = new Mock<ICurrencyExchangeService>();

                // Replace the real currency exchange service with the mock
                services.RemoveAll<ICurrencyExchangeService>();
                services.AddSingleton(MockCurrencyExchangeService.Object);

                // Ensure the database is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            });
        }

        public void ResetMockService()
        {
            MockCurrencyExchangeService?.Reset();
        }
    }
}
