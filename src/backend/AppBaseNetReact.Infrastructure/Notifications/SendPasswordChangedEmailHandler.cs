using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class SendPasswordChangedEmailHandler
    : INotificationHandler<PasswordChangedNotification>,
      INotificationHandler<PasswordResetNotification>
{
    private readonly IEmailService _email;
    private readonly ILogger<SendPasswordChangedEmailHandler> _logger;

    public SendPasswordChangedEmailHandler(
        IEmailService email,
        ILogger<SendPasswordChangedEmailHandler> logger)
    {
        _email = email;
        _logger = logger;
    }

    public Task Handle(PasswordChangedNotification notification, CancellationToken ct)
        => SendAsync(notification.Email, notification.FirstName, "password change", ct);

    public Task Handle(PasswordResetNotification notification, CancellationToken ct)
        => SendAsync(notification.Email, notification.FirstName, "password reset", ct);

    private async Task SendAsync(string email, string firstName, string context, CancellationToken ct)
    {
        try
        {
            await _email.SendPasswordChangedEmailAsync(email, firstName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-changed email ({Context}) to {Email}", context, email);
        }
    }
}
