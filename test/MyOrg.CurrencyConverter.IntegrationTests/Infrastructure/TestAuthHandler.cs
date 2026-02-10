using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MyOrg.CurrencyConverter.IntegrationTests.Infrastructure
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if the request has a test authorization header
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
            }

            var authHeader = Request.Headers["Authorization"].ToString();

            // Allow "TestAuth" as a valid token for integration tests
            if (!authHeader.StartsWith("Bearer TestAuth"))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
            }

            // Create claims for the test user
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
