using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class OnboardingEmailResentEmailHandler : INotificationHandler<OnboardingEmailResentNotification>
{
    private readonly IEmailService _email;
    private readonly EmailRenderer _renderer;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<OnboardingEmailResentEmailHandler> _logger;

    public OnboardingEmailResentEmailHandler(
        IEmailService email,
        EmailRenderer renderer,
        IOptions<EmailOptions> emailOptions,
        ILogger<OnboardingEmailResentEmailHandler> logger)
    {
        _email = email;
        _renderer = renderer;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task Handle(OnboardingEmailResentNotification notification, CancellationToken ct)
    {
        if (!_emailOptions.Templates.TryGetValue("EmailResend", out var config))
        {
            _logger.LogWarning("EmailResend template not configured; skipping send to {Email}", notification.Email);
            return;
        }

        try
        {
            var confirmationLink =
                $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/confirm-email?token={notification.NewConfirmationToken}";

            var vars = new Dictionary<string, string>
            {
                ["UserName"] = notification.FirstName,
                ["ConfirmationLink"] = confirmationLink,
                ["Year"] = DateTime.UtcNow.Year.ToString()
            };

            var htmlBody = _renderer.Render(config.TemplateFile, vars);
            await _email.SendEmailAsync(notification.Email, config.Subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send onboarding resend email to {Email}", notification.Email);
        }
    }
}
