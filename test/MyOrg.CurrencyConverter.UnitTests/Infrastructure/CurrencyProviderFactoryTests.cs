using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyOrg.CurrencyConverter.API.Core.Configuration;
using MyOrg.CurrencyConverter.API.Core.Enums;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Infrastructure;
using MyOrg.CurrencyConverter.API.Infrastructure.Factories;

namespace MyOrg.CurrencyConverter.UnitTests.Infrastructure;

public class CurrencyProviderFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<CurrencyProviderFactory>> _mockLogger;
    private readonly CurrencyProviderSettings _settings;
    private readonly CurrencyProviderFactory _factory;

    public CurrencyProviderFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<CurrencyProviderFactory>>();

        _settings = new CurrencyProviderSettings
        {
            ActiveProvider = CurrencyProviderType.Frankfurter,
            Frankfurter = new FrankfurterProviderSettings
            {
                BaseUrl = "https://api.frankfurter.app"
            }
        };

        var options = Options.Create(_settings);

        _factory = new CurrencyProviderFactory(
            _mockServiceProvider.Object,
            options,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CurrencyProviderFactory(
            null!,
            Options.Create(_settings),
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CurrencyProviderFactory(
            _mockServiceProvider.Object,
            null!,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CreateProvider with Type Tests

    [Fact]
    public void CreateProvider_FrankfurterType_ReturnsFrankfurterProvider()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IHttpClientFactory)))
            .Returns(mockHttpClientFactory.Object);

        // Act
        var provider = _factory.CreateProvider(CurrencyProviderType.Frankfurter);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<FrankfurterCurrencyProvider>();
    }

    [Fact]
    public void CreateProvider_UnsupportedType_ThrowsNotSupportedException()
    {
        // Act & Assert
        var action = () => _factory.CreateProvider((CurrencyProviderType)5);

        action.Should().Throw<NotSupportedException>()
            .WithMessage("*not yet implemented*");
    }

    #endregion

    #region CreateProvider without Type Tests

    [Fact]
    public void CreateProvider_UsesActiveProviderFromSettings()
    {
        // Arrange
        _settings.ActiveProvider = CurrencyProviderType.Frankfurter;
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IHttpClientFactory)))
            .Returns(mockHttpClientFactory.Object);

        // Act
        var provider = _factory.CreateProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<FrankfurterCurrencyProvider>();
    }

    [Fact]
    public void CreateProvider_DefaultProvider_CreatesFrankfurterProvider()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IHttpClientFactory)))
            .Returns(mockHttpClientFactory.Object);

        // Act
        var provider = _factory.CreateProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<FrankfurterCurrencyProvider>();
    }

    #endregion

    #region GetAvailableProviders Tests

    [Fact]
    public void GetAvailableProviders_ReturnsOnlyImplementedProviders()
    {
        // Act
        var availableProviders = _factory.GetAvailableProviders().ToList();

        // Assert
        availableProviders.Should().NotBeEmpty();
        availableProviders.Should().Contain(CurrencyProviderType.Frankfurter);
        availableProviders.Should().HaveCount(1); // Only Frankfurter is implemented
    }

    [Fact]
    public void GetAvailableProviders_DoesNotContainUnimplementedProviders()
    {
        // Act
        var availableProviders = _factory.GetAvailableProviders().ToList();

        // Assert
        availableProviders.Should().NotContain(CurrencyProviderType.Invalid);
    }

    #endregion

    #region Integration-Style Tests

    [Fact]
    public void CreateProvider_WithRealServiceProvider_CreatesWorkingProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var factory = new CurrencyProviderFactory(
            serviceProvider,
            Options.Create(_settings),
            serviceProvider.GetRequiredService<ILogger<CurrencyProviderFactory>>());

        // Act
        var provider = factory.CreateProvider(CurrencyProviderType.Frankfurter);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<FrankfurterCurrencyProvider>();
    }

    #endregion
}
