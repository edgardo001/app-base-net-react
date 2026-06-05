using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class AccountLockedEmailHandler : INotificationHandler<AccountLockedNotification>
{
    private readonly IEmailService _email;
    private readonly ILogger<AccountLockedEmailHandler> _logger;

    public AccountLockedEmailHandler(IEmailService email, ILogger<AccountLockedEmailHandler> logger)
    {
        _email = email;
        _logger = logger;
    }

    public async Task Handle(AccountLockedNotification notification, CancellationToken ct)
    {
        try
        {
            var resetLink = $"{notification.FrontendUrl}/reset-password";
            await _email.SendAccountLockedEmailAsync(
                notification.Email,
                notification.FirstName,
                notification.LockoutMinutes,
                resetLink,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account-locked email to {Email}", notification.Email);
        }
    }
}
