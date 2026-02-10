using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MyOrg.CurrencyConverter.API.Infrastructure.Data;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace MyOrg.CurrencyConverter.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog (skip in Testing environment to avoid conflicts with test framework)
            if (builder.Environment.EnvironmentName != "Testing")
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateBootstrapLogger();

                Log.Information("Starting Currency Converter API");

                // Replace default logging with Serilog
                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.WithEnvironmentName());
            }

            // Add services to the container.
            builder.Services.AddControllers();

            // Add CORS
            StartupHelper.AddCorsPolicy(builder);

            // Add Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            StartupHelper.AddSwagger(builder);

            // Add OpenTelemetry
            StartupHelper.AddOpenTelemetry(builder);

            // Add Rate Limiting
            StartupHelper.AddRateLimiting(builder);

            // Register application services with Polly resilience policies
            ServicesProviderHelper.AddAppServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Add Serilog request logging (skip in Testing environment)
            if (app.Environment.EnvironmentName != "Testing")
            {
                app.UseSerilogRequestLogging(options =>
                {
                    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    };
                });
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Converter API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            // CORS must come before authentication and authorization
            app.UseCors("CorsPolicy");

            // Rate limiting must come before authentication
            app.UseRateLimiter();

            // Authentication must come before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Map Identity API endpoints (/register, /login, /refresh, etc.)
            app.MapIdentityApi<ApplicationUser>();

            // Map application controllers
            app.MapControllers();

            // Auto-apply database migrations on startup (useful for Docker/containerized deployments)
            await StartupHelper.ApplyMigrationsAndSeedDataAsync(app);

            LogMessage(app, "Currency Converter API started successfully");

            app.Run();

            if (builder.Environment.EnvironmentName != "Testing")
            {
                Log.CloseAndFlush();
            }
        }

        private static void LogMessage(WebApplication app, string message)
        {
            if (app.Environment.EnvironmentName != "Testing")
            {
                Log.Information("Currency Converter API started successfully");
            }
        }
    }

     
}
