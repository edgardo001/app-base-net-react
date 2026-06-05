using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class UserLoggedOutAuditHandler : INotificationHandler<UserLoggedOutNotification>
{
    private readonly IAuditService _audit;

    public UserLoggedOutAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(UserLoggedOutNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "UserLoggedOut", "RefreshToken", null,
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            null, ct);
    }
}
