namespace BackendMoviles.Infrastructure.Security;

public sealed class JwtTokenOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "HelpMomApi";
    public string Audience { get; set; } = "HelpMomFlutter";
    public int ExpirationMinutes { get; set; } = 60;
}