using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class UserLoginFailedAuditHandler : INotificationHandler<UserLoginFailedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<UserLoginFailedAuditHandler> _logger;

    public UserLoginFailedAuditHandler(IAuditService audit, ILogger<UserLoginFailedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(UserLoginFailedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "UserLoginFailed", "User", null,
            null, null, null,
            notification.IpAddress,
            "unknown",
            $"Login failed for {notification.Email}: {notification.Reason}",
            ct);

        _logger.LogWarning("Login failed for {Email} from {Ip}: {Reason}",
            notification.Email, notification.IpAddress, notification.Reason);
    }
}
