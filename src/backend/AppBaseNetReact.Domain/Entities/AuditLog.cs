using AppBaseNetReact.Domain.Common;

namespace AppBaseNetReact.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string? Details { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action, string entityType, string? entityId,
        string? oldValues, string? newValues,
        string ipAddress, string userAgent,
        Guid? userId = null, string? details = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
    }
}
