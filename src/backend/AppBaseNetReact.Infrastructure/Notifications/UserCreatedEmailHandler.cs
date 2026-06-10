using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class UserCreatedEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _email;
    private readonly EmailRenderer _renderer;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<UserCreatedEmailHandler> _logger;

    public UserCreatedEmailHandler(
        IEmailService email,
        EmailRenderer renderer,
        IOptions<EmailOptions> emailOptions,
        ILogger<UserCreatedEmailHandler> logger)
    {
        _email = email;
        _renderer = renderer;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        if (!_emailOptions.Templates.TryGetValue("EmailConfirmation", out var config))
        {
            _logger.LogWarning("EmailConfirmation template not configured; skipping send to {Email}", notification.Email);
            return;
        }

        try
        {
            var confirmationLink =
                $"{notification.FrontendBaseUrl.TrimEnd('/')}/confirm-email?token={notification.ConfirmationToken}";

            var vars = new Dictionary<string, string>
            {
                ["UserName"] = notification.FirstName,
                ["ConfirmationLink"] = confirmationLink,
                ["TemporaryPassword"] = notification.TemporaryPassword,
                ["Year"] = DateTime.UtcNow.Year.ToString()
            };

            var htmlBody = _renderer.Render(config.TemplateFile, vars);
            await _email.SendEmailAsync(notification.Email, config.Subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send user-created email to {Email}", notification.Email);
        }
    }
}
