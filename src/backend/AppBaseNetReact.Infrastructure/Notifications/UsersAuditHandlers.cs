using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class UserCreatedAuditHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<UserCreatedAuditHandler> _logger;

    public UserCreatedAuditHandler(IAuditService audit, ILogger<UserCreatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "UserCreated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"User '{notification.Email}' created",
            ct);

        _logger.LogInformation("User {UserId} created from {Ip}", notification.UserId, notification.IpAddress);
    }
}

public sealed class UserUpdatedAuditHandler : INotificationHandler<UserUpdatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<UserUpdatedAuditHandler> _logger;

    public UserUpdatedAuditHandler(IAuditService audit, ILogger<UserUpdatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(UserUpdatedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "UserUpdated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"User '{notification.Email}' updated",
            ct);

        _logger.LogInformation("User {UserId} updated from {Ip}", notification.UserId, notification.IpAddress);
    }
}

public sealed class UserDeletedAuditHandler : INotificationHandler<UserDeletedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<UserDeletedAuditHandler> _logger;

    public UserDeletedAuditHandler(IAuditService audit, ILogger<UserDeletedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(UserDeletedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "UserDeleted", "User", notification.UserId.ToString(),
            null, null, notification.DeletedBy,
            notification.IpAddress,
            notification.UserAgent,
            $"User '{notification.Email}' soft-deleted",
            ct);

        _logger.LogInformation("User {UserId} deleted from {Ip}", notification.UserId, notification.IpAddress);
    }
}

public sealed class UserActivatedAuditHandler : INotificationHandler<UserActivatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<UserActivatedAuditHandler> _logger;

    public UserActivatedAuditHandler(IAuditService audit, ILogger<UserActivatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(UserActivatedNotification notification, CancellationToken ct)
    {
        var action = notification.IsActive ? "UserActivated" : "UserDeactivated";
        await _audit.LogAsync(
            action, "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"User '{notification.Email}' {(notification.IsActive ? "activated" : "deactivated")}",
            ct);

        _logger.LogInformation("User {UserId} {Action} from {Ip}", notification.UserId, action.ToLowerInvariant(), notification.IpAddress);
    }
}

public sealed class UserDeactivatedAuditHandler : INotificationHandler<UserDeactivatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<UserDeactivatedAuditHandler> _logger;

    public UserDeactivatedAuditHandler(IAuditService audit, ILogger<UserDeactivatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(UserDeactivatedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "UserDeactivated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"User '{notification.Email}' deactivated",
            ct);

        _logger.LogInformation("User {UserId} deactivated from {Ip}", notification.UserId, notification.IpAddress);
    }
}

public sealed class PasswordResetByAdminAuditHandler : INotificationHandler<PasswordResetByAdminNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<PasswordResetByAdminAuditHandler> _logger;

    public PasswordResetByAdminAuditHandler(IAuditService audit, ILogger<PasswordResetByAdminAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(PasswordResetByAdminNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "PasswordResetByAdmin", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"Password reset by admin for user '{notification.Email}'",
            ct);

        _logger.LogInformation("Password reset by admin for user {UserId} from {Ip}", notification.UserId, notification.IpAddress);
    }
}

public sealed class TokensRevokedAuditHandler : INotificationHandler<TokensRevokedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<TokensRevokedAuditHandler> _logger;

    public TokensRevokedAuditHandler(IAuditService audit, ILogger<TokensRevokedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(TokensRevokedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "TokensRevoked", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"All tokens revoked for user '{notification.UserId}'",
            ct);

        _logger.LogInformation("Tokens revoked for user {UserId} from {Ip}", notification.UserId, notification.IpAddress);
    }
}

public sealed class AvatarUpdatedAuditHandler : INotificationHandler<AvatarUpdatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<AvatarUpdatedAuditHandler> _logger;

    public AvatarUpdatedAuditHandler(IAuditService audit, ILogger<AvatarUpdatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(AvatarUpdatedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "AvatarUpdated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            $"Avatar updated for user '{notification.Email}' to '{notification.FileName}'",
            ct);

        _logger.LogInformation("Avatar updated for user {UserId} from {Ip}", notification.UserId, notification.IpAddress);
    }
}
