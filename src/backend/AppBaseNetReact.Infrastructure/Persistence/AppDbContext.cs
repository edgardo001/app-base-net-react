using Microsoft.EntityFrameworkCore;
using AppBaseNetReact.Domain.Common;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence.Configurations;

namespace AppBaseNetReact.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // Se usa entry.Property().CurrentValue en vez de entry.Entity.UpdatedAt =
    // porque BaseEntity.UpdatedAt tiene protected set (encapsulamiento domain).
    // EF Core puede modificar propiedades con setter protegido via el entry,
    // pero solo si se accede a traves de Property().CurrentValue.
    // Esto mantiene el dominio puro sin setters publicos innecesarios.
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
            }
        }
        return await base.SaveChangesAsync(ct);
    }
}
