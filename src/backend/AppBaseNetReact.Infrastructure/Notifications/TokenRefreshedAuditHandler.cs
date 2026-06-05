using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class TokenRefreshedAuditHandler : INotificationHandler<TokenRefreshedNotification>
{
    private readonly IAuditService _audit;

    public TokenRefreshedAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(TokenRefreshedNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "TokenRefreshed", "RefreshToken", null,
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            null, ct);
    }
}
