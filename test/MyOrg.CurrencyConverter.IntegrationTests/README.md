# Currency Converter - Integration Tests

This project contains integration tests for the Currency Converter API endpoints, specifically focusing on the `CurrencyController`.

## Overview

Integration tests verify that the API endpoints work correctly when the application components are integrated together. Unlike unit tests that test individual components in isolation, integration tests validate the entire request/response pipeline.

## Test Infrastructure

### CustomWebApplicationFactory

The `CustomWebApplicationFactory` class extends `WebApplicationFactory<Program>` to create an in-memory test server with the following configurations:

- **In-Memory Database**: Uses Entity Framework's in-memory database instead of a real PostgreSQL database
- **Disabled Features**: Caches (Redis), rate limiting, and OpenTelemetry are disabled for faster test execution
- **Mock Currency Provider**: The external currency provider API is mocked to avoid dependencies on external services
- **Test Authentication**: Uses a custom authentication handler instead of real JWT authentication

### TestAuthHandler

The `TestAuthHandler` provides a simplified authentication mechanism for integration tests:

- Accepts any request with `Authorization: Bearer TestAuth` header
- Creates test claims with admin role
- Bypasses real JWT validation

## Test Coverage

### CurrencyController Endpoints

1. **GET /api/currency/latest/{baseCurrency}**
   - Valid base currency returns OK with rates
   - Invalid base currency returns BadRequest
   - Unauthenticated request returns Unauthorized

2. **GET /api/currency/convert**
   - Valid parameters return conversion result
   - Negative amount returns BadRequest
   - Zero amount returns BadRequest
   - Same currency conversion works correctly

3. **GET /api/currency/rate**
   - Valid currencies return rate data
   - Invalid from currency returns BadRequest
   - Invalid to currency returns BadRequest

4. **GET /api/currency/historical**
   - Valid parameters return paged results
   - End date before start date returns BadRequest
   - Invalid page number returns BadRequest
   - Default pagination works correctly

5. **Error Handling**
   - External API down returns ServiceUnavailable (503)
   - External API error returns BadGateway (502)

## Running the Tests

### Prerequisites

- .NET 10.0 SDK
- No external dependencies (Redis, PostgreSQL) required for integration tests

### Command Line

```bash
# Navigate to the test project directory
cd test/MyOrg.CurrencyConverter.IntegrationTests

# Restore packages
dotnet restore

# Run all integration tests
dotnet test

# Run with verbose output
dotnet test --verbosity detailed

# Run specific test
dotnet test --filter "FullyQualifiedName~GetLatestRates_WithValidBaseCurrency_ReturnsOk"

# Run tests in a specific class
dotnet test --filter "FullyQualifiedName~CurrencyControllerIntegrationTests"
```

### Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All" to execute all tests
3. Or right-click specific tests to run individually

### Visual Studio Code

1. Install C# Dev Kit extension
2. Open the Testing panel
3. Run tests from the test explorer

## Best Practices

### Test Isolation

Each test method is isolated and does not depend on other tests. The mock currency provider is reset between test runs to ensure clean state.

### Descriptive Test Names

Test names follow the pattern: `MethodName_Scenario_ExpectedBehavior`
- Example: `GetLatestRates_WithInvalidBaseCurrency_ReturnsBadRequest`

### Arrange-Act-Assert Pattern

All tests follow the AAA pattern:
```csharp
// Arrange - Set up test data and mocks
var baseCurrency = "USD";
_factory.MockCurrencyProvider?.Setup(...);

// Act - Execute the API call
var response = await _client.GetAsync($"/api/currency/latest/{baseCurrency}");

// Assert - Verify the results
response.StatusCode.Should().Be(HttpStatusCode.OK);
```

### Fluent Assertions

Tests use FluentAssertions for readable and expressive assertions:
```csharp
result.Should().NotBeNull();
result!.Base.Should().Be(baseCurrency);
result.Rates.Should().ContainKey("EUR");
```

## Extending the Tests

### Adding New Endpoint Tests

1. Add new test methods to `CurrencyControllerIntegrationTests.cs`
2. Follow the existing naming convention
3. Set up appropriate mocks for the currency provider
4. Use the authenticated `_client` for protected endpoints

### Testing Different Scenarios

To test different scenarios:
1. Configure the mock provider with different responses
2. Use `_factory.MockCurrencyProvider?.Setup(...)` to define behavior
3. Test both success and failure paths
4. Verify correct HTTP status codes and response bodies

### Testing Other Controllers

1. Create a new test class (e.g., `RoleControllerIntegrationTests.cs`)
2. Inherit from `IClassFixture<CustomWebApplicationFactory>`
3. Follow the same patterns as `CurrencyControllerIntegrationTests`

## Troubleshooting

### Tests Fail Due to Missing Dependencies

- Ensure the API project builds successfully
- Check that all NuGet packages are restored

### Authentication Issues

- Verify the `TestAuthHandler` is properly registered
- Ensure test requests include the `Authorization: Bearer TestAuth` header

### Mock Setup Issues

- Check that the mock provider is properly configured
- Verify that method signatures match the actual provider interface
- Use `.VerifyAll()` on mocks to ensure all setups are called

## CI/CD Integration

These integration tests can run in CI/CD pipelines without any external dependencies:

```yaml
# Example GitHub Actions workflow
- name: Run Integration Tests
  run: dotnet test test/MyOrg.CurrencyConverter.IntegrationTests --no-build --verbosity normal
```

No need to spin up Docker containers for PostgreSQL or Redis!

## Performance Considerations

Integration tests are slower than unit tests because they:
- Create an in-memory web server
- Process full HTTP requests
- Execute middleware pipeline
- Use in-memory database

For faster feedback, run unit tests during development and integration tests before committing or in CI/CD pipelines.

## Related Documentation

- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
