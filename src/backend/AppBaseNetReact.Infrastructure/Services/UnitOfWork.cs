using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Persistence;
using AppBaseNetReact.Infrastructure.Persistence.Repositories;

namespace AppBaseNetReact.Infrastructure.Services;

// UnitOfWork implementa el patron de agregacion de repositorios.
// No reemplaza el Unit of Work de EF Core (DbContext.SaveChangesAsync), sino que expone
// los repositorios como propiedades con inicializacion lazy (??=). Esto previene constructor
// explosion en los controllers (6+ repos parametros) y mantiene un solo SaveChangesAsync.
// Al usar IUnitOfWork como dependencia, los tests pueden mockear repos individualmente.
// IDisposable asegura limpieza del DbContext en el lifetime del request (Scoped DI).
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AppDbContext _context;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IPermissionRepository? _permissions;
    private IRefreshTokenRepository? _refreshTokens;
    private IAuditLogRepository? _auditLogs;
    private ILoginAttemptRepository? _loginAttempts;
    private IPasswordHistoryRepository? _passwordHistories;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public ILoginAttemptRepository LoginAttempts => _loginAttempts ??= new LoginAttemptRepository(_context);
    public IPasswordHistoryRepository PasswordHistories => _passwordHistories ??= new PasswordHistoryRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public void Dispose()
    {
        _context.Dispose();
    }
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
