namespace AppBaseNetReact.Application.Features.Roles.Commands.UpdatePermissions;

public sealed record UpdatePermissionsOutcome(UpdatePermissionsResult Result);

public sealed record UpdatePermissionsResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static UpdatePermissionsResult Success() => new(true);
    public static UpdatePermissionsResult NotFound() => new(false, "NotFound", "Role not found");
}
