using FluentAssertions;
using MyOrg.CurrencyConverter.API.Core.Models;

namespace MyOrg.CurrencyConverter.UnitTests.Core.Models
{
    public class CurrencyRatesTests
    {
        #region ConvertAmount Tests

        [Fact]
        public void ConvertAmount_ValidInputs_ReturnsCorrectConversion()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.92m },
                    { "GBP", 0.79m }
                }
            };

            // Act
            var result = currencyRates.ConvertAmount(100m, "EUR");

            // Assert
            result.Should().Be(92m);
        }

        [Fact]
        public void ConvertAmount_ZeroAmount_ReturnsZero()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
            };

            // Act
            var result = currencyRates.ConvertAmount(0m, "EUR");

            // Assert
            result.Should().Be(0m);
        }

        [Fact]
        public void ConvertAmount_DecimalAmount_CalculatesCorrectly()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
            };

            // Act
            var result = currencyRates.ConvertAmount(123.45m, "EUR");

            // Assert
            result.Should().Be(113.574m);
        }

        [Fact]
        public void ConvertAmount_MissingTargetCurrency_ThrowsInvalidOperationException()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
            };

            // Act
            var action = () => currencyRates.ConvertAmount(100m, "GBP");

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Exchange rate for GBP not found");
        }

        [Fact]
        public void ConvertAmount_NullRates_ThrowsInvalidOperationException()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = null!
            };

            // Act
            var action = () => currencyRates.ConvertAmount(100m, "EUR");

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Exchange rate for EUR not found");
        }

        #endregion

        #region GetRate Tests

        [Fact]
        public void GetRate_ValidCurrency_ReturnsRate()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.92m },
                    { "GBP", 0.79m }
                }
            };

            // Act
            var result = currencyRates.GetRate("EUR");

            // Assert
            result.Should().Be(0.92m);
        }

        [Fact]
        public void GetRate_MissingCurrency_ThrowsInvalidOperationException()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
            };

            // Act
            var action = () => currencyRates.GetRate("GBP");

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Exchange rate for GBP not found");
        }

        [Fact]
        public void GetRate_NullRates_ThrowsInvalidOperationException()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = null!
            };

            // Act
            var action = () => currencyRates.GetRate("EUR");

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Exchange rate for EUR not found");
        }

        #endregion

        #region HasRate Tests

        [Fact]
        public void HasRate_ExistingCurrency_ReturnsTrue()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.92m },
                    { "GBP", 0.79m }
                }
            };

            // Act
            var result = currencyRates.HasRate("EUR");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasRate_MissingCurrency_ReturnsFalse()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } }
            };

            // Act
            var result = currencyRates.HasRate("GBP");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasRate_NullRates_ReturnsFalse()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = null!
            };

            // Act
            var result = currencyRates.HasRate("EUR");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasRate_EmptyRates_ReturnsFalse()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal>()
            };

            // Act
            var result = currencyRates.HasRate("EUR");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void CurrencyRates_MultipleConversions_WorkCorrectly()
        {
            // Arrange
            var currencyRates = new CurrencyRates
            {
                Base = "USD",
                Date = "2024-01-01",
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.92m },
                    { "GBP", 0.79m },
                    { "JPY", 110.5m }
                }
            };

            // Act & Assert
            currencyRates.ConvertAmount(100m, "EUR").Should().Be(92m);
            currencyRates.ConvertAmount(100m, "GBP").Should().Be(79m);
            currencyRates.ConvertAmount(100m, "JPY").Should().Be(11050m);

            currencyRates.HasRate("EUR").Should().BeTrue();
            currencyRates.HasRate("CHF").Should().BeFalse();

            currencyRates.GetRate("JPY").Should().Be(110.5m);
        }

        #endregion
    }
}
