using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class OnboardingEmailResentAuditHandler : INotificationHandler<OnboardingEmailResentNotification>
{
    private readonly IAuditService _audit;

    public OnboardingEmailResentAuditHandler(IAuditService audit)
    {
        _audit = audit;
    }

    public Task Handle(OnboardingEmailResentNotification notification, CancellationToken ct)
    {
        return _audit.LogAsync(
            "OnboardingEmailResent", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent, null, ct);
    }
}
