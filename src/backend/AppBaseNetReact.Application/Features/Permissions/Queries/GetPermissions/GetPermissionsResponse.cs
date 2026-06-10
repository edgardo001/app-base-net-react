namespace AppBaseNetReact.Application.Features.Permissions.Queries.GetPermissions;

public sealed record GetPermissionsResponse
{
    public IReadOnlyList<PermissionItemDto> Items { get; init; } = [];
}

public sealed record PermissionItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
