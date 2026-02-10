namespace MyOrg.CurrencyConverter.API.Core.DTOs.Requests
{
    public class ConvertCurrencyRequest
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
