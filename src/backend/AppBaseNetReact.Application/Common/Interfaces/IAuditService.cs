namespace AppBaseNetReact.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action, string entityType, string? entityId,
        string? oldValues, string? newValues,
        Guid? userId, string ipAddress, string userAgent,
        string? details = null,
        CancellationToken ct = default);
}
