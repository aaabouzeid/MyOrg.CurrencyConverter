using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MyOrg.CurrencyConverter.API.Infrastructure.Data;
using Serilog;
using Serilog.Events;

namespace MyOrg.CurrencyConverter.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog early (bootstrap logger)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting Currency Converter API");

                var builder = WebApplication.CreateBuilder(args);

                // Replace default logging with Serilog
                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.WithEnvironmentName());

                // Add services to the container.

                builder.Services.AddControllers();

                // Add Swagger/OpenAPI
                builder.Services.AddEndpointsApiExplorer();
                AddSwagger(builder);

                // Add Rate Limiting
                AddRateLimiting(builder);

                // Register application services with Polly resilience policies
                ServicesProviderHelper.AddAppServices(builder.Services, builder.Configuration);

                var app = builder.Build();

                // Add Serilog request logging
                app.UseSerilogRequestLogging(options =>
                {
                    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    };
                });

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
                var autoMigrate = app.Configuration.GetValue<bool>("AutoMigrate", false);
                if (autoMigrate)
                {
                    Log.Information("Auto-migration enabled. Applying database migrations...");
                    using (var scope = app.Services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        dbContext.Database.Migrate();
                        Log.Information("Database migrations applied successfully");

                        // Seed roles
                        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                        await RoleSeeder.SeedRolesAsync(roleManager, app.Configuration);

                        // Optionally seed default admin user
                        var seedDefaultAdmin = app.Configuration.GetValue<bool>("SeedDefaultAdmin", false);
                        if (seedDefaultAdmin)
                        {
                            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                            await RoleSeeder.SeedDefaultAdminAsync(userManager, app.Configuration);
                        }
                    }
                }

                Log.Information("Currency Converter API started successfully");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void AddSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Currency Converter API",
                    Version = "v1",
                    Description = "API for currency conversion with JWT authentication"
                });

                // Define the Bearer security scheme
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token in the format: {your token}"
                });

                // Make Bearer authentication required globally using a lambda function
                options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference("Bearer", doc, null),
                            new List<string>()
                        }
                    });
            });
        }

        private static void AddRateLimiting(WebApplicationBuilder builder)
        {
            var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
            var rateLimitEnabled = rateLimitConfig.GetValue<bool>("Enabled", true);

            if (!rateLimitEnabled)
            {
                Log.Information("Rate limiting is disabled");
                return;
            }

            // Read configuration values
            var permitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
            var windowSeconds = rateLimitConfig.GetValue<int>("WindowSeconds", 60);

            builder.Services.AddRateLimiter(options =>
            {
                // Default rejection response
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                        ? (double?)retryAfterValue.TotalSeconds
                        : null;

                    var response = new
                    {
                        error = "Too many requests",
                        message = "Rate limit exceeded. Please try again later.",
                        retryAfter = retryAfter != null ? $"{retryAfter:F0} seconds" : "Please wait before retrying"
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);

                    Log.Warning("Rate limit exceeded for {IPAddress} on {Path}",
                        context.HttpContext.Connection.RemoteIpAddress,
                        context.HttpContext.Request.Path);
                };

                // Global fixed window limiter (applied to all requests)
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowSeconds),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });

            Log.Information("Rate limiting enabled: {PermitLimit} requests per {WindowSeconds} seconds per IP address",
                permitLimit, windowSeconds);
        }
    }
}
