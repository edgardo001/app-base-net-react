namespace AppBaseNetReact.Application.Features.Admin.Queries.GetAuditLog;

public sealed record GetAuditLogResponse
{
    public IReadOnlyList<AuditLogItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed record AuditLogItemDto
{
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string? Details { get; init; }
    public Guid? UserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
