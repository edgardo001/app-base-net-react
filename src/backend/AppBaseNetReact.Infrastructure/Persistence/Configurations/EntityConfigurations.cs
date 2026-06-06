using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.SecurityStamp).HasMaxLength(64).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.AvatarPath).HasMaxLength(500);
        builder.Property(u => u.EmailConfirmationToken).HasMaxLength(256);
        builder.Property(u => u.ConcurrencyToken).IsConcurrencyToken();

        // Partial unique indexes: enforce uniqueness only among ACTIVE users
        // (DeletedAt IS NULL). Soft-deleted users do not occupy the email,
        // so a re-creation with the same email succeeds. The query filter
        // hides soft-deleted users from the application, so the controller
        // pre-check (GetByEmailAsync) returns null after deactivation.
        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL")
            .HasDatabaseName("IX_Users_NormalizedEmail");
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL")
            .HasDatabaseName("IX_Users_Email");
        builder.HasIndex(u => u.IsActive).HasDatabaseName("IX_Users_IsActive");
        builder.HasIndex(u => u.DeletedAt).HasDatabaseName("IX_Users_DeletedAt");
            // Soft delete global filter: excluye usuarios eliminados de TODAS las queries.
        // Para incluir eliminados, usar .IgnoreQueryFilters().
        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(r => r.NormalizedName).IsUnique().HasDatabaseName("IX_Roles_NormalizedName");
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("IX_Permissions_Code");
        builder.HasIndex(p => p.Module).HasDatabaseName("IX_Permissions_Module");
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Query filter en dependencia: EF Core requiere que las navigaciones requeridas
        // tengan filtros consistentes con el filter de User (DeletedAt == null).
        // Sin esto, EF Core advierte que la navigation User podria ser null.
        builder.HasQueryFilter(ur => ur.User.DeletedAt == null);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Property(rp => rp.Granted).IsRequired();

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(rt => rt.DeviceInfo).HasMaxLength(500);
        builder.Property(rt => rt.IpAddress).HasMaxLength(50);
        builder.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(256);
        builder.Property(rt => rt.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(rt => rt.JwtId).IsUnique().HasDatabaseName("IX_RefreshTokens_JwtId");
        builder.HasIndex(rt => rt.TokenHash).HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Query filter redundante: EF Core valida que las navigations requeridas no sean filtradas.
        // Sin este filtro, EF advierte que User podria ser null por el global filter en UserConfiguration.
        // Ver: https://learn.microsoft.com/ef/core/querying/filters#required-navigation-filters
        builder.HasQueryFilter(rt => rt.User.DeletedAt == null);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(al => al.Id);

        builder.Property(al => al.Action).HasMaxLength(100).IsRequired();
        builder.Property(al => al.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(al => al.EntityId).HasMaxLength(100);
        builder.Property(al => al.IpAddress).HasMaxLength(50).IsRequired();
        builder.Property(al => al.UserAgent).HasMaxLength(500).IsRequired();

        builder.HasIndex(al => al.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
        builder.HasIndex(al => al.UserId).HasDatabaseName("IX_AuditLogs_UserId");
        builder.HasIndex(al => al.Action).HasDatabaseName("IX_AuditLogs_Action");
    }
}

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");
        builder.HasKey(la => la.Id);

        builder.Property(la => la.Email).HasMaxLength(256).IsRequired();
        builder.Property(la => la.IpAddress).HasMaxLength(50).IsRequired();
        builder.Property(la => la.FailureReason).HasMaxLength(200);

        builder.HasIndex(la => la.CreatedAt).HasDatabaseName("IX_LoginAttempts_CreatedAt");
        builder.HasIndex(la => new { la.IpAddress, la.CreatedAt }).HasDatabaseName("IX_LoginAttempts_IpAddress_CreatedAt");
    }
}
