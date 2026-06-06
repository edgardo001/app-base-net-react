namespace AppBaseNetReact.Infrastructure.Services;

public class EmailOptions
{
    public string Provider { get; set; } = "None";
    public SmtpSettings Smtp { get; set; } = new();
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    public Dictionary<string, EmailTemplateConfig> Templates { get; set; } = [];
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool QueueEnabled { get; set; }
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class EmailTemplateConfig
{
    public string Subject { get; set; } = string.Empty;
    public string TemplateFile { get; set; } = string.Empty;
}
