namespace MyOrg.CurrencyConverter.API.Core.Models;

/// <summary>
/// JWT authentication configuration settings
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing tokens (min 256 bits / 32 characters for HS256)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (typically the application name or domain)
    /// </summary>
    public string Issuer { get; set; } = "CurrencyConverter.API";

    /// <summary>
    /// Token audience (typically the API consumers)
    /// </summary>
    public string Audience { get; set; } = "CurrencyConverter.Clients";

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Whether to validate token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Clock skew for token expiration (in minutes)
    /// Allows small time differences between servers
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;
}
