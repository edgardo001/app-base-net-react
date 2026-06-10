namespace AppBaseNetReact.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleOutcome(CreateRoleResult Result);

public sealed record CreateRoleResult(bool IsSuccess, Guid? RoleId = null, string? Name = null, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static CreateRoleResult Success(Guid roleId, string name) => new(true, roleId, name);
    public static CreateRoleResult DuplicateName() => new(false, null, null, "DuplicateName", "Role name already exists");
}
