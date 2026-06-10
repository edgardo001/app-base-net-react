using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Commands.UpdatePermissions;

public sealed record UpdatePermissionsCommand(
    Guid RoleId,
    List<PermissionAssignmentDto> Permissions,
    string IpAddress,
    string UserAgent) : IRequest<UpdatePermissionsOutcome>;

public sealed record PermissionAssignmentDto(Guid PermissionId, bool Granted);
