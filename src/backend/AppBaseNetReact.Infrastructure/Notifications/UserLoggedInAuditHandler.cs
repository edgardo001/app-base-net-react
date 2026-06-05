using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class UserLoggedInAuditHandler : INotificationHandler<UserLoggedInNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<UserLoggedInAuditHandler> _logger;

    public UserLoggedInAuditHandler(IAuditService audit, ILogger<UserLoggedInAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(UserLoggedInNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "UserLoggedIn", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"User {notification.Email} logged in",
            ct);

        _logger.LogInformation("User {UserId} logged in from {Ip}", notification.UserId, notification.IpAddress);
    }
}
