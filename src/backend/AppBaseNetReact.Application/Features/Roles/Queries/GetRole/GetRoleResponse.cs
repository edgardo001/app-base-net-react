namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRole;

public sealed record GetRoleResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsSystem { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<RolePermissionDto> Permissions { get; init; } = [];
}

public sealed record RolePermissionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public bool Granted { get; init; }
}
