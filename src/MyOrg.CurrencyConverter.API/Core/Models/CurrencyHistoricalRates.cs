namespace MyOrg.CurrencyConverter.API.Core.Models
{
    public class CurrencyHistoricalRates
    {
        public string Base { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;

        public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; } = [];
    }
}
