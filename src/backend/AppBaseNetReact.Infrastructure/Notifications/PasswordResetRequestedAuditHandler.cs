using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class PasswordResetRequestedAuditHandler : INotificationHandler<PasswordResetRequestedNotification>
{
    private readonly IAuditService _audit;

    public PasswordResetRequestedAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(PasswordResetRequestedNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "PasswordResetRequested", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            "Reset token generated", ct);
    }
}
