using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Common;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence;

namespace AppBaseNetReact.Infrastructure.Persistence.Repositories;

// GenericRepository<T> requiere where T : BaseEntity para poder llamar
// SoftDelete() en DeleteAsync y para que T? sea nullable reference type
// correctamente. La interfaz IRepository<T> usa la misma constraint
// para que el compilador pueda hacer matching de return types (Task<T?>).
// Los repositorios concretos (UserRepository, RoleRepository, etc.) se
// registran por separado en DI aunque hereden de GenericRepository.
public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int page, int pageSize,
        Expression<Func<T, bool>>? filter = null,
        string? sortBy = null, bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsQueryable();

        if (filter != null)
            query = query.Where(filter);

        var totalCount = await query.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query = sortDesc
                ? query.OrderByDescending(e => EF.Property<object>(e, sortBy))
                : query.OrderBy(e => EF.Property<object>(e, sortBy));
        }

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        entity.SoftDelete(null);
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
    {
        if (filter == null) return await _dbSet.CountAsync(ct);
        return await _dbSet.CountAsync(filter, ct);
    }
}

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);

    public async Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> GetUsersByRoleAsync(Guid roleId, CancellationToken ct = default)
        => await _dbSet
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
            .ToListAsync(ct);

    public async Task<User?> GetByEmailConfirmationTokenAsync(string token, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token, ct);
}

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(AppDbContext context) : base(context) { }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(r => r.NormalizedName == name.ToUpperInvariant(), ct);

    public async Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
}

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
        => await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task RevokeAllForUserAsync(Guid userId, Guid? revokedBy = null, CancellationToken ct = default)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revoke(revokedBy);
    }

    public async Task RevokeAllGlobalAsync(Guid? revokedBy = null, CancellationToken ct = default)
    {
        var tokens = await _dbSet
            .Where(rt => rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revoke(revokedBy);
    }
}

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid userId, int limit = 50, CancellationToken ct = default)
        => await _dbSet
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
}

public class PermissionRepository : GenericRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(AppDbContext context) : base(context) { }
}

public class LoginAttemptRepository : GenericRepository<LoginAttempt>, ILoginAttemptRepository
{
    public LoginAttemptRepository(AppDbContext context) : base(context) { }
}
