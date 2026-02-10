namespace MyOrg.CurrencyConverter.API.Core.Models.Requests
{
    public class GetExchangeRateRequest
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }
}
