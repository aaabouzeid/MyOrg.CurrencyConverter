namespace MyOrg.CurrencyConverter.API.Core.Configuration;

/// <summary>
/// Rate limiting configuration settings
/// </summary>
public class RateLimitSettings
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}
