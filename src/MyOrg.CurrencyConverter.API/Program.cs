using Serilog;
using Serilog.Events;

namespace MyOrg.CurrencyConverter.API
{
    public class Program
    {
        public static void Main(string[] args)
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
                builder.Services.AddSwaggerGen();

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

                app.UseAuthorization();


                app.MapControllers();

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
    }
}
