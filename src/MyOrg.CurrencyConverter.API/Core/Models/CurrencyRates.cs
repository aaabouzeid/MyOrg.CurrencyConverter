namespace MyOrg.CurrencyConverter.API.Core.Models
{
    public class CurrencyRates
    { 
        public string Base { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;

        public Dictionary<string, decimal> Rates { get; set; } = [];
    }
}
