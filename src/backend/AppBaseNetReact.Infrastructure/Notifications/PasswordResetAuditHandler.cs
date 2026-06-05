using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class PasswordResetAuditHandler : INotificationHandler<PasswordResetNotification>
{
    private readonly IAuditService _audit;

    public PasswordResetAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(PasswordResetNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "PasswordReset", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            "Password reset via token", ct);
    }
}
