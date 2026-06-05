using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class PasswordChangedAuditHandler : INotificationHandler<PasswordChangedNotification>
{
    private readonly IAuditService _audit;

    public PasswordChangedAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(PasswordChangedNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "PasswordChanged", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent, null, ct);
    }
}
