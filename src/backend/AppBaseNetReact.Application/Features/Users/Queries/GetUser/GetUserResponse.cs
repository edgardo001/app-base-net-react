namespace AppBaseNetReact.Application.Features.Users.Queries.GetUser;

public sealed record GetUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? AvatarPath { get; init; }
    public bool IsActive { get; init; }
    public bool EmailConfirmed { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime? LastPasswordChangeAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<RoleDto> Roles { get; init; } = [];
}

public sealed record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
