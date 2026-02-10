using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MyOrg.CurrencyConverter.API.Infrastructure.Data;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace MyOrg.CurrencyConverter.API
{
    public static class StartupHelper
    {
        public static void AddCorsPolicy(WebApplicationBuilder builder)
        {
            var corsConfig = builder.Configuration.GetSection("Cors");
            var corsEnabled = corsConfig.GetValue<bool>("Enabled", true);

            if (!corsEnabled)
            {
                Log.Information("CORS is disabled");
                return;
            }

            // Read allowed origins from configuration
            var allowedOrigins = corsConfig.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "http://localhost:3001" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials() // Required for authentication cookies/tokens
                          .WithExposedHeaders("Content-Disposition"); // For file downloads if needed
                });
            });

            Log.Information("CORS enabled for origins: {AllowedOrigins}", string.Join(", ", allowedOrigins));
        }

        public static void AddSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Currency Converter API",
                    Version = "v1.0",
                    Description = "API for currency conversion with JWT authentication and versioning support. All endpoints are prefixed with /api/v1/"
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

        public static void AddRateLimiting(WebApplicationBuilder builder)
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

        public static void AddOpenTelemetry(WebApplicationBuilder builder)
        {
            var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
            var otelEnabled = otelConfig.GetValue<bool>("Enabled", true);

            if (!otelEnabled)
            {
                Log.Information("OpenTelemetry is disabled");
                return;
            }

            var serviceName = otelConfig.GetValue<string>("ServiceName") ?? "CurrencyConverter.API";
            var serviceVersion = otelConfig.GetValue<string>("ServiceVersion") ?? "1.0.0";

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource =>
                {
                    resource.AddService(
                        serviceName: serviceName,
                        serviceVersion: serviceVersion);
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            // Enrich spans with additional information
                            options.EnrichWithHttpRequest = (activity, httpRequest) =>
                            {
                                // Add Client IP
                                var clientIp = httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                                activity.SetTag("client.ip", clientIp);

                                // Add Forwarded IP if behind proxy
                                if (httpRequest.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                                {
                                    activity.SetTag("client.forwarded_ip", forwardedFor.ToString());
                                }

                                // Add HTTP Request Details
                                activity.SetTag("http.request.content_length", httpRequest.ContentLength ?? 0);
                                activity.SetTag("http.request.content_type", httpRequest.ContentType ?? "none");

                                // Add Query String (be careful with sensitive data)
                                if (httpRequest.QueryString.HasValue)
                                {
                                    activity.SetTag("http.query_string", httpRequest.QueryString.Value);
                                }

                                // Add User Agent
                                if (httpRequest.Headers.TryGetValue("User-Agent", out var userAgent))
                                {
                                    activity.SetTag("http.user_agent", userAgent.ToString());
                                }
                            };

                            options.EnrichWithHttpResponse = (activity, httpResponse) =>
                            {
                                // Add HTTP Response Details
                                activity.SetTag("http.response.content_length", httpResponse.ContentLength ?? 0);
                                activity.SetTag("http.response.content_type", httpResponse.ContentType ?? "none");

                                // Add Response Headers if needed
                                if (httpResponse.Headers.TryGetValue("Content-Encoding", out var encoding))
                                {
                                    activity.SetTag("http.response.encoding", encoding.ToString());
                                }

                                // Add Client ID (User Identity) - AFTER authentication middleware has run
                                var httpContext = httpResponse.HttpContext;
                                var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
                                activity.SetTag("client.authenticated", isAuthenticated);

                                if (isAuthenticated)
                                {
                                    // Debug: Log all available claims to help troubleshoot
                                    var allClaims = httpContext.User.Claims
                                        .Select(c => $"{c.Type}={c.Value}")
                                        .ToList();

                                    // For JWT tokens, try to get user ID from claims in order of preference
                                    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                        ?? httpContext.User.FindFirst("sub")?.Value
                                        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                        ?? httpContext.User.FindFirst("name")?.Value
                                        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                        ?? httpContext.User.FindFirst("email")?.Value
                                        ?? httpContext.User.Identity?.Name
                                        ?? "authenticated-unknown";

                                    activity.SetTag("client.id", userId);

                                    // Add User Roles
                                    var roles = httpContext.User.Claims
                                        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role
                                                 || c.Type == "role"
                                                 || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                                        .Select(c => c.Value)
                                        .ToList();

                                    if (roles.Any())
                                    {
                                        activity.SetTag("client.roles", string.Join(",", roles));
                                    }
                                }
                                else
                                {
                                    activity.SetTag("client.id", "anonymous");
                                }
                            };

                            // Record exceptions
                            options.RecordException = true;
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            // Track outgoing HTTP requests (to external APIs)
                            options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                            {
                                activity.SetTag("http.client.method", httpRequestMessage.Method.ToString());
                                activity.SetTag("http.client.url", httpRequestMessage.RequestUri?.ToString() ?? "unknown");
                            };

                            options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                            {
                                activity.SetTag("http.client.status_code", (int)httpResponseMessage.StatusCode);
                            };
                        });

                    // Add Console Exporter for development
                    var consoleEnabled = otelConfig.GetValue<bool>("ConsoleExporter:Enabled", true);
                    if (consoleEnabled)
                    {
                        tracing.AddConsoleExporter();
                        Log.Information("OpenTelemetry Console Exporter enabled");
                    }

                    // Add OTLP Exporter for production (Jaeger, Zipkin, etc.)
                    var otlpEnabled = otelConfig.GetValue<bool>("OtlpExporter:Enabled", false);
                    if (otlpEnabled)
                    {
                        var otlpEndpoint = otelConfig.GetValue<string>("OtlpExporter:Endpoint") ?? "http://localhost:4317";
                        tracing.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(otlpEndpoint);
                        });
                        Log.Information("OpenTelemetry OTLP Exporter enabled: {Endpoint}", otlpEndpoint);
                    }
                });

            Log.Information("OpenTelemetry enabled for service: {ServiceName} v{ServiceVersion}", serviceName, serviceVersion);
        }

        public static async Task ApplyMigrationsAndSeedDataAsync(WebApplication app)
        {
            // Skip migrations in test environment
            if (app.Environment.EnvironmentName == "Testing")
            {
                return;
            }

            var autoMigrate = app.Configuration.GetValue<bool>("AutoMigrate", false);
            if (!autoMigrate)
            {
                return;
            }

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
    }
}
