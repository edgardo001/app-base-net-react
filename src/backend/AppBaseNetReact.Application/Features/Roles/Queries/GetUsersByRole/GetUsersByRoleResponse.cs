namespace AppBaseNetReact.Application.Features.Roles.Queries.GetUsersByRole;

public sealed record GetUsersByRoleResponse
{
    public IReadOnlyList<UserByRoleDto> Users { get; init; } = [];
}

public sealed record UserByRoleDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
