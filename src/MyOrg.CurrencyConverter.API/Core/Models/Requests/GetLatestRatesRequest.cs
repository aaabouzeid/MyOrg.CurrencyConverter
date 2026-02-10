namespace MyOrg.CurrencyConverter.API.Core.Models.Requests
{
    public class GetLatestRatesRequest
    {
        public string BaseCurrency { get; set; } = string.Empty;
    }
}
