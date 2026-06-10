using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Notifications;

namespace AppBaseNetReact.Infrastructure.Notifications;

public sealed class RoleCreatedAuditHandler : INotificationHandler<RoleCreatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<RoleCreatedAuditHandler> _logger;

    public RoleCreatedAuditHandler(IAuditService audit, ILogger<RoleCreatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(RoleCreatedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "RoleCreated", "Role", notification.RoleId.ToString(),
            null, null, null,
            notification.IpAddress,
            notification.UserAgent,
            $"Role '{notification.RoleName}' created",
            ct);

        _logger.LogInformation("Role {RoleId} created from {Ip}", notification.RoleId, notification.IpAddress);
    }
}

public sealed class RoleUpdatedAuditHandler : INotificationHandler<RoleUpdatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<RoleUpdatedAuditHandler> _logger;

    public RoleUpdatedAuditHandler(IAuditService audit, ILogger<RoleUpdatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(RoleUpdatedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "RoleUpdated", "Role", notification.RoleId.ToString(),
            System.Text.Json.JsonSerializer.Serialize(new { Name = notification.OldName }),
            System.Text.Json.JsonSerializer.Serialize(new { Name = notification.NewName }),
            null,
            notification.IpAddress,
            notification.UserAgent,
            $"Role '{notification.OldName}' updated to '{notification.NewName}'",
            ct);

        _logger.LogInformation("Role {RoleId} updated from {Ip}", notification.RoleId, notification.IpAddress);
    }
}

public sealed class RoleDeletedAuditHandler : INotificationHandler<RoleDeletedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<RoleDeletedAuditHandler> _logger;

    public RoleDeletedAuditHandler(IAuditService audit, ILogger<RoleDeletedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(RoleDeletedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "RoleDeleted", "Role", notification.RoleId.ToString(),
            null, null, notification.DeletedBy,
            notification.IpAddress,
            notification.UserAgent,
            $"Role '{notification.RoleName}' deleted",
            ct);

        _logger.LogInformation("Role {RoleId} deleted from {Ip}", notification.RoleId, notification.IpAddress);
    }
}

public sealed class RolePermissionsUpdatedAuditHandler : INotificationHandler<RolePermissionsUpdatedNotification>
{
    private readonly IAuditService _audit;
    private readonly ILogger<RolePermissionsUpdatedAuditHandler> _logger;

    public RolePermissionsUpdatedAuditHandler(IAuditService audit, ILogger<RolePermissionsUpdatedAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(RolePermissionsUpdatedNotification notification, CancellationToken ct)
    {
        await _audit.LogAsync(
            "RolePermissionsUpdated", "Role", notification.RoleId.ToString(),
            null, null, null,
            notification.IpAddress,
            notification.UserAgent,
            $"Permissions updated for role '{notification.RoleName}'",
            ct);

        _logger.LogInformation("Permissions updated for role {RoleId} from {Ip}", notification.RoleId, notification.IpAddress);
    }
}
