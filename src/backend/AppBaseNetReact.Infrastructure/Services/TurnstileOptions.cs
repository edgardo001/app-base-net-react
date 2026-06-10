namespace AppBaseNetReact.Infrastructure.Services;

public class TurnstileOptions
{
    public string Provider { get; set; } = "None";
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
