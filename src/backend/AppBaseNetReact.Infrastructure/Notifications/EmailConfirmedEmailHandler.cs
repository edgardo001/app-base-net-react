using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class EmailConfirmedEmailHandler : INotificationHandler<EmailConfirmedNotification>
{
    private readonly IEmailService _email;
    private readonly ILogger<EmailConfirmedEmailHandler> _logger;

    public EmailConfirmedEmailHandler(IEmailService email, ILogger<EmailConfirmedEmailHandler> logger)
    {
        _email = email;
        _logger = logger;
    }

    public async Task Handle(EmailConfirmedNotification notification, CancellationToken ct)
    {
        try
        {
            await _email.SendWelcomeEmailAsync(
                notification.Email,
                notification.FirstName,
                notification.LoginLink,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", notification.Email);
        }
    }
}
