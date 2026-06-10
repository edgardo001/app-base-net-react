namespace AppBaseNetReact.Application.Features.Roles.Commands.UpdateRole;

public sealed record UpdateRoleOutcome(UpdateRoleResult Result);

public sealed record UpdateRoleResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static UpdateRoleResult Success() => new(true);
    public static UpdateRoleResult NotFound() => new(false, "NotFound", "Role not found");
    public static UpdateRoleResult CannotModifySystemRole() => new(false, "CannotModifySystemRole", "Cannot modify system roles");
}
