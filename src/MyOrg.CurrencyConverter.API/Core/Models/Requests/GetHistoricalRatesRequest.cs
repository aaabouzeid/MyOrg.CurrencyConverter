namespace MyOrg.CurrencyConverter.API.Core.Models.Requests
{
    public class GetHistoricalRatesRequest
    {
        public string BaseCurrency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
