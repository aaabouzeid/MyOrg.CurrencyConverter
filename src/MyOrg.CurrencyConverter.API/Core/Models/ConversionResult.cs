namespace MyOrg.CurrencyConverter.API.Core.Models
{
    public class ConversionResult
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Date { get; set; } = string.Empty;
    }
}
