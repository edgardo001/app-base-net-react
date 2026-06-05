using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class EmailConfirmedAuditHandler : INotificationHandler<EmailConfirmedNotification>
{
    private readonly IAuditService _audit;

    public EmailConfirmedAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(EmailConfirmedNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "EmailConfirmed", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent, null, ct);
    }
}
