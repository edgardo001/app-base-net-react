namespace AppBaseNetReact.Application.Features.Profile.Queries.GetProfile;

public sealed record GetProfileResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? AvatarPath { get; init; }
}
