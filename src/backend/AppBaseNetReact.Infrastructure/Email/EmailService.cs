using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly EmailRenderer _renderer;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> options,
        EmailRenderer renderer,
        ILogger<EmailService> logger)
    {
        _options = options.Value;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (_options.Provider == "None")
        {
            _logger.LogInformation("[Email-Dev] To: {To} | Subject: {Subject} | Body: {Body}",
                to, subject, htmlBody);
            return;
        }

        if (string.IsNullOrEmpty(_options.Smtp.Host))
            throw new InvalidOperationException("SMTP host is not configured.");

        var message = CreateMessage(to, subject, htmlBody);

        var attempt = 0;
        while (true)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port,
                    MailKit.Security.SecureSocketOptions.StartTls, ct);
                await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
                return;
            }
            catch (Exception ex) when (attempt < _options.RetryCount)
            {
                attempt++;
                _logger.LogWarning(ex, "Failed to send email to {To} (attempt {Attempt}/{RetryCount})",
                    to, attempt, _options.RetryCount);
                await Task.Delay(TimeSpan.FromSeconds(_options.RetryDelaySeconds), ct);
            }
        }
    }

    private MimeMessage CreateMessage(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var body = new TextPart("html") { Text = htmlBody };
        message.Body = body;

        return message;
    }

    public async Task SendAccountLockedEmailAsync(
        string to, string userName, int lockoutMinutes, string resetLink, CancellationToken ct = default)
    {
        if (!_options.Templates.TryGetValue("AccountLocked", out var config))
        {
            _logger.LogWarning("AccountLocked template not configured; skipping email to {To}", to);
            return;
        }

        var vars = new Dictionary<string, string>
        {
            ["UserName"] = userName,
            ["LockoutMinutes"] = lockoutMinutes.ToString(),
            ["ResetLink"] = resetLink,
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        var htmlBody = _renderer.Render(config.TemplateFile, vars);
        await SendEmailAsync(to, config.Subject, htmlBody, ct);
    }

    public async Task SendPasswordChangedEmailAsync(
        string to, string userName, CancellationToken ct = default)
    {
        if (!_options.Templates.TryGetValue("PasswordChanged", out var config))
        {
            _logger.LogWarning("PasswordChanged template not configured; skipping email to {To}", to);
            return;
        }

        var vars = new Dictionary<string, string>
        {
            ["UserName"] = userName,
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        var htmlBody = _renderer.Render(config.TemplateFile, vars);
        await SendEmailAsync(to, config.Subject, htmlBody, ct);
    }

    public async Task SendWelcomeEmailAsync(
        string to, string userName, string loginLink, CancellationToken ct = default)
    {
        if (!_options.Templates.TryGetValue("Welcome", out var config))
        {
            _logger.LogWarning("Welcome template not configured; skipping email to {To}", to);
            return;
        }

        var vars = new Dictionary<string, string>
        {
            ["UserName"] = userName,
            ["LoginLink"] = loginLink,
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        var htmlBody = _renderer.Render(config.TemplateFile, vars);
        await SendEmailAsync(to, config.Subject, htmlBody, ct);
    }
}
