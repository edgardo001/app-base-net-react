namespace AppBaseNetReact.Application.Features.Profile.Queries.GetActivity;

public sealed record GetActivityResponse
{
    public IReadOnlyList<ActivityItemDto> Items { get; init; } = [];
}

public sealed record ActivityItemDto
{
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? Details { get; init; }
    public DateTime CreatedAt { get; init; }
}
