namespace MyOrg.CurrencyConverter.API.Core.Models
{
    public class CurrencyRates
    {
        public string Base { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;

        public Dictionary<string, decimal> Rates { get; set; } = [];

        /// <summary>
        /// Converts an amount from the base currency to the target currency
        /// </summary>
        /// <param name="amount">The amount to convert</param>
        /// <param name="targetCurrency">The target currency code</param>
        /// <returns>The converted amount</returns>
        /// <exception cref="InvalidOperationException">Thrown when the exchange rate is not available</exception>
        public decimal ConvertAmount(decimal amount, string targetCurrency)
        {
            if (Rates == null || !Rates.ContainsKey(targetCurrency))
                throw new InvalidOperationException($"Exchange rate for {targetCurrency} not found");

            return amount * Rates[targetCurrency];
        }

        /// <summary>
        /// Gets the exchange rate for a specific target currency
        /// </summary>
        /// <param name="targetCurrency">The target currency code</param>
        /// <returns>The exchange rate</returns>
        /// <exception cref="InvalidOperationException">Thrown when the exchange rate is not available</exception>
        public decimal GetRate(string targetCurrency)
        {
            if (Rates == null || !Rates.ContainsKey(targetCurrency))
                throw new InvalidOperationException($"Exchange rate for {targetCurrency} not found");

            return Rates[targetCurrency];
        }

        /// <summary>
        /// Checks if an exchange rate exists for the target currency
        /// </summary>
        /// <param name="targetCurrency">The target currency code</param>
        /// <returns>True if the rate exists, false otherwise</returns>
        public bool HasRate(string targetCurrency)
        {
            return Rates != null && Rates.ContainsKey(targetCurrency);
        }
    }
}
