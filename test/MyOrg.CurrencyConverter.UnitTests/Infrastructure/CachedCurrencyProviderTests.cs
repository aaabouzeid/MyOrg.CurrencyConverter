using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyOrg.CurrencyConverter.API.Core.Configuration;
using MyOrg.CurrencyConverter.API.Core.Interfaces;
using MyOrg.CurrencyConverter.API.Core.Models;
using MyOrg.CurrencyConverter.API.Infrastructure;

namespace MyOrg.CurrencyConverter.UnitTests.Infrastructure;

public class CachedCurrencyProviderTests
{
    private readonly Mock<ICurrencyProvider> _mockInnerProvider;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<CachedCurrencyProvider>> _mockLogger;
    private readonly CacheSettings _cacheSettings;
    private readonly CachedCurrencyProvider _cachedProvider;

    public CachedCurrencyProviderTests()
    {
        _mockInnerProvider = new Mock<ICurrencyProvider>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CachedCurrencyProvider>>();

        _cacheSettings = new CacheSettings
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            Ttl = new CacheTtlSettings
            {
                LatestRatesMinutes = 30,
                ExchangeRateMinutes = 30
            },
            ThrowOnFailure = false
        };

        var options = Options.Create(_cacheSettings);

        _cachedProvider = new CachedCurrencyProvider(
            _mockInnerProvider.Object,
            _mockCacheService.Object,
            options,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullInnerProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CachedCurrencyProvider(
            null!,
            _mockCacheService.Object,
            Options.Create(_cacheSettings),
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("innerProvider");
    }

    [Fact]
    public void Constructor_NullCacheService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CachedCurrencyProvider(
            _mockInnerProvider.Object,
            null!,
            Options.Create(_cacheSettings),
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("cacheService");
    }

    #endregion

    #region GetLatestExchangeRates Tests

    [Fact]
    public async Task GetLatestExchangeRates_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var baseCurrency = "USD";
        var cachedRates = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CurrencyRates>("currency:latest:USD", default))
            .ReturnsAsync(cachedRates);

        // Act
        var result = await _cachedProvider.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeSameAs(cachedRates);
        _mockInnerProvider.Verify(x => x.GetLatestExchangeRates(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<CurrencyRates>(), It.IsAny<TimeSpan>(), default), Times.Never);
    }

    [Fact]
    public async Task GetLatestExchangeRates_CacheMiss_CallsProviderAndCachesResult()
    {
        // Arrange
        var baseCurrency = "EUR";
        var providerRates = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { "USD", 1.18m } }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CurrencyRates>("currency:latest:EUR", default))
            .ReturnsAsync((CurrencyRates?)null);

        _mockInnerProvider
            .Setup(x => x.GetLatestExchangeRates(baseCurrency))
            .ReturnsAsync(providerRates);

        // Act
        var result = await _cachedProvider.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeSameAs(providerRates);
        _mockInnerProvider.Verify(x => x.GetLatestExchangeRates(baseCurrency), Times.Once);
        _mockCacheService.Verify(
            x => x.SetAsync("currency:latest:EUR", providerRates, TimeSpan.FromMinutes(30), default),
            Times.Once);
    }

    [Fact]
    public async Task GetLatestExchangeRates_CacheDisabled_CallsProviderDirectly()
    {
        // Arrange
        _cacheSettings.Enabled = false;
        var baseCurrency = "GBP";
        var providerRates = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { "USD", 1.25m } }
        };

        _mockInnerProvider
            .Setup(x => x.GetLatestExchangeRates(baseCurrency))
            .ReturnsAsync(providerRates);

        // Act
        var result = await _cachedProvider.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeSameAs(providerRates);
        _mockInnerProvider.Verify(x => x.GetLatestExchangeRates(baseCurrency), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<CurrencyRates>(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task GetLatestExchangeRates_CacheRetrievalFails_FallsBackToProvider()
    {
        // Arrange
        var baseCurrency = "JPY";
        var providerRates = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { "USD", 0.0091m } }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CurrencyRates>("currency:latest:JPY", default))
            .ThrowsAsync(new InvalidOperationException("Redis connection failed"));

        _mockInnerProvider
            .Setup(x => x.GetLatestExchangeRates(baseCurrency))
            .ReturnsAsync(providerRates);

        // Act
        var result = await _cachedProvider.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeSameAs(providerRates);
        _mockInnerProvider.Verify(x => x.GetLatestExchangeRates(baseCurrency), Times.Once);
    }

    [Fact]
    public async Task GetLatestExchangeRates_CacheSetFails_StillReturnsProviderData()
    {
        // Arrange
        var baseCurrency = "CAD";
        var providerRates = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { "USD", 0.79m } }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CurrencyRates>("currency:latest:CAD", default))
            .ReturnsAsync((CurrencyRates?)null);

        _mockInnerProvider
            .Setup(x => x.GetLatestExchangeRates(baseCurrency))
            .ReturnsAsync(providerRates);

        _mockCacheService
            .Setup(x => x.SetAsync("currency:latest:CAD", providerRates, It.IsAny<TimeSpan>(), default))
            .ThrowsAsync(new InvalidOperationException("Redis write failed"));

        // Act
        var result = await _cachedProvider.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeSameAs(providerRates);
    }

    [Fact]
    public async Task GetLatestExchangeRates_UsesCaseInsensitiveCacheKey()
    {
        // Arrange
        var baseCurrency = "usd";
        var cachedRates = new CurrencyRates
        {
            Base = "USD",
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CurrencyRates>("currency:latest:USD", default))
            .ReturnsAsync(cachedRates);

        // Act
        var result = await _cachedProvider.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeSameAs(cachedRates);
        _mockCacheService.Verify(x => x.GetAsync<CurrencyRates>("currency:latest:USD", default), Times.Once);
    }

    #endregion

    #region GetExchangeRate Tests

    [Fact]
    public async Task GetExchangeRate_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var baseCurrency = "USD";
        var targetCurrency = "EUR";
        var cachedRate = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { targetCurrency, 0.85m } }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CurrencyRates>("currency:pair:USD:EUR", default))
            .ReturnsAsync(cachedRate);

        // Act
        var result = await _cachedProvider.GetExchangeRate(baseCurrency, targetCurrency);

        // Assert
        result.Should().BeSameAs(cachedRate);
        _mockInnerProvider.Verify(x => x.GetExchangeRate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetExchangeRate_CacheMiss_CallsProviderAndCachesResult()
    {
        // Arrange
        var baseCurrency = "GBP";
        var targetCurrency = "JPY";
        var providerRate = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { targetCurrency, 160.5m } }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CurrencyRates>("currency:pair:GBP:JPY", default))
            .ReturnsAsync((CurrencyRates?)null);

        _mockInnerProvider
            .Setup(x => x.GetExchangeRate(baseCurrency, targetCurrency))
            .ReturnsAsync(providerRate);

        // Act
        var result = await _cachedProvider.GetExchangeRate(baseCurrency, targetCurrency);

        // Assert
        result.Should().BeSameAs(providerRate);
        _mockInnerProvider.Verify(x => x.GetExchangeRate(baseCurrency, targetCurrency), Times.Once);
        _mockCacheService.Verify(
            x => x.SetAsync("currency:pair:GBP:JPY", providerRate, TimeSpan.FromMinutes(30), default),
            Times.Once);
    }

    [Fact]
    public async Task GetExchangeRate_CacheDisabled_CallsProviderDirectly()
    {
        // Arrange
        _cacheSettings.Enabled = false;
        var baseCurrency = "EUR";
        var targetCurrency = "USD";
        var providerRate = new CurrencyRates
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, decimal> { { targetCurrency, 1.18m } }
        };

        _mockInnerProvider
            .Setup(x => x.GetExchangeRate(baseCurrency, targetCurrency))
            .ReturnsAsync(providerRate);

        // Act
        var result = await _cachedProvider.GetExchangeRate(baseCurrency, targetCurrency);

        // Assert
        result.Should().BeSameAs(providerRate);
        _mockInnerProvider.Verify(x => x.GetExchangeRate(baseCurrency, targetCurrency), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<CurrencyRates>(It.IsAny<string>(), default), Times.Never);
    }

    #endregion

    #region GetHistoricalExchangeRates Tests

    [Fact]
    public async Task GetHistoricalExchangeRates_NoCaching_AlwaysCallsProvider()
    {
        // Arrange
        var baseCurrency = "USD";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var historicalRates = new CurrencyHistoricalRates
        {
            Base = baseCurrency,
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            Rates = new Dictionary<string, Dictionary<string, decimal>>()
        };

        _mockInnerProvider
            .Setup(x => x.GetHistoricalExchangeRates(baseCurrency, startDate, endDate, 1, 10))
            .ReturnsAsync((historicalRates, 30));

        // Act
        var result = await _cachedProvider.GetHistoricalExchangeRates(baseCurrency, startDate, endDate, 1, 10);

        // Assert
        result.rates.Should().BeSameAs(historicalRates);
        result.totalDays.Should().Be(30);
        _mockInnerProvider.Verify(x => x.GetHistoricalExchangeRates(baseCurrency, startDate, endDate, 1, 10), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<It.IsAnyType>(It.IsAny<string>(), default), Times.Never);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<TimeSpan>(), default), Times.Never);
    }

    #endregion
}
