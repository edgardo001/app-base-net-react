using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class PasswordResetRequestedEmailHandler
    : INotificationHandler<PasswordResetRequestedNotification>
{
    private readonly IEmailService _email;
    private readonly ILogger<PasswordResetRequestedEmailHandler> _logger;

    public PasswordResetRequestedEmailHandler(
        IEmailService email,
        ILogger<PasswordResetRequestedEmailHandler> logger)
    {
        _email = email;
        _logger = logger;
    }

    public async Task Handle(PasswordResetRequestedNotification notification, CancellationToken ct)
    {
        try
        {
            await _email.SendPasswordResetEmailAsync(
                notification.Email, notification.FirstName, notification.ResetLink, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset email to {Email}", notification.Email);
        }
    }
}
