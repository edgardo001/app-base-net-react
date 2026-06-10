namespace AppBaseNetReact.Application.Features.Roles.Commands.DeleteRole;

public sealed record DeleteRoleOutcome(DeleteRoleResult Result);

public sealed record DeleteRoleResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static DeleteRoleResult Success() => new(true);
    public static DeleteRoleResult NotFound() => new(false, "NotFound", "Role not found");
    public static DeleteRoleResult CannotDeleteSystemRole() => new(false, "CannotDeleteSystemRole", "Cannot delete system roles");
}
