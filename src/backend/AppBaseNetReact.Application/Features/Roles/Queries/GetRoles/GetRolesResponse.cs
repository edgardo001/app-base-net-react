namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRoles;

public sealed record GetRolesResponse
{
    public IReadOnlyList<RoleListItemDto> Items { get; init; } = [];
}

public sealed record RoleListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsSystem { get; init; }
    public DateTime CreatedAt { get; init; }
}
