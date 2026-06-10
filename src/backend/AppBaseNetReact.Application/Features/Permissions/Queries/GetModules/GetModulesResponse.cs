namespace AppBaseNetReact.Application.Features.Permissions.Queries.GetModules;

public sealed record GetModulesResponse
{
    public IReadOnlyList<ModuleGroupDto> Modules { get; init; } = [];
}

public sealed record ModuleGroupDto
{
    public string Module { get; init; } = string.Empty;
    public IReadOnlyList<ModulePermissionDto> Permissions { get; init; } = [];
}

public sealed record ModulePermissionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
