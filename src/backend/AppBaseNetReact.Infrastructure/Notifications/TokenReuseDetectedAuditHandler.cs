using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class TokenReuseDetectedAuditHandler : INotificationHandler<TokenReuseDetectedNotification>
{
    private readonly IAuditService _audit;

    public TokenReuseDetectedAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(TokenReuseDetectedNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "TokenReuseDetected", "RefreshToken", notification.RefreshTokenId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            "Compromised refresh token detected — all sessions revoked", ct);
    }
}
