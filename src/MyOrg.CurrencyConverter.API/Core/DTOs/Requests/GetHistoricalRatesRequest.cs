namespace MyOrg.CurrencyConverter.API.Core.DTOs.Requests
{
    public class GetHistoricalRatesRequest
    {
        public string BaseCurrency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
