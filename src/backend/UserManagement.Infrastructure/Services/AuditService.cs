using UserManagement.Application.Common.Interfaces;
using UserManagement.Domain.Entities;

namespace UserManagement.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;

    public AuditService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task LogAsync(
        string action, string entityType, string? entityId,
        string? oldValues, string? newValues,
        Guid? userId, string ipAddress, string userAgent,
        string? details = null,
        CancellationToken ct = default)
    {
        var log = AuditLog.Create(
            action, entityType, entityId,
            oldValues, newValues,
            ipAddress, userAgent,
            userId, details);

        await _uow.AuditLogs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
