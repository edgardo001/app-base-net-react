using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class PasswordResetByAdminEmailHandler : INotificationHandler<PasswordResetByAdminNotification>
{
    private readonly IEmailService _email;
    private readonly EmailRenderer _renderer;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<PasswordResetByAdminEmailHandler> _logger;

    public PasswordResetByAdminEmailHandler(
        IEmailService email,
        EmailRenderer renderer,
        IOptions<EmailOptions> emailOptions,
        ILogger<PasswordResetByAdminEmailHandler> logger)
    {
        _email = email;
        _renderer = renderer;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task Handle(PasswordResetByAdminNotification notification, CancellationToken ct)
    {
        if (!_emailOptions.Templates.TryGetValue("TemporaryPassword", out var config))
        {
            _logger.LogWarning("TemporaryPassword template not configured; skipping send to {Email}", notification.Email);
            return;
        }

        try
        {
            var vars = new Dictionary<string, string>
            {
                ["UserName"] = notification.FirstName,
                ["TempPassword"] = notification.TemporaryPassword,
                ["LoginLink"] = notification.LoginLink,
                ["Year"] = DateTime.UtcNow.Year.ToString()
            };

            var htmlBody = _renderer.Render(config.TemplateFile, vars);
            await _email.SendEmailAsync(notification.Email, config.Subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin password-reset email to {Email}", notification.Email);
        }
    }
}
