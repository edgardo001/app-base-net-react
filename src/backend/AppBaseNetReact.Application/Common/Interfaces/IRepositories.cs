using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailConfirmationTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetUsersByRoleAsync(Guid roleId, CancellationToken ct = default);
}

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken ct = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, Guid? revokedBy = null, CancellationToken ct = default);
    Task RevokeAllGlobalAsync(Guid? revokedBy = null, CancellationToken ct = default);
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid userId, int limit = 50, CancellationToken ct = default);
}

public interface IPermissionRepository : IRepository<Permission>
{
}

public interface ILoginAttemptRepository : IRepository<LoginAttempt>
{
}

public interface IPasswordHistoryRepository : IRepository<PasswordHistory>
{
    Task DeleteOldestForUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRecentHashesAsync(Guid userId, int count, CancellationToken ct = default);
}

public interface IExternalLoginRepository : IRepository<ExternalLogin>
{
    Task<ExternalLogin?> GetByProviderAsync(string provider, string providerId, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPermissionRepository Permissions { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IAuditLogRepository AuditLogs { get; }
    ILoginAttemptRepository LoginAttempts { get; }
    IPasswordHistoryRepository PasswordHistories { get; }
    IExternalLoginRepository ExternalLogins { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
