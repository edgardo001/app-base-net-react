using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Notifications;

public sealed record RoleCreatedNotification(
    Guid RoleId,
    string RoleName,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record RoleUpdatedNotification(
    Guid RoleId,
    string OldName,
    string NewName,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record RoleDeletedNotification(
    Guid RoleId,
    string RoleName,
    Guid? DeletedBy,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record RolePermissionsUpdatedNotification(
    Guid RoleId,
    string RoleName,
    string IpAddress,
    string UserAgent) : INotification;
