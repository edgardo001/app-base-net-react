using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence;

namespace AppBaseNetReact.Infrastructure.Services;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        await context.Database.MigrateAsync();

        var anySeeded = await context.Permissions.AnyAsync();
        if (anySeeded)
        {
            logger.LogInformation("Database already seeded, ensuring missing seed data...");
        }
        else
        {
            logger.LogInformation("Seeding database...");
        }

        var permissionDefs = new List<(string Code, string Name, string Module, string Description)>
        {
            ("users:list", "List Users", "Users", "View list of users"),
            ("users:create", "Create Users", "Users", "Create new users"),
            ("users:edit", "Edit Users", "Users", "Edit existing users"),
            ("users:delete", "Delete Users", "Users", "Soft delete users"),
            ("users:activate", "Activate/Deactivate Users", "Users", "Toggle user active status"),
            ("users:reset-password", "Reset User Password", "Users", "Reset another user's password"),
            ("users:revoke-tokens", "Revoke User Tokens", "Users", "Revoke sessions for a user"),
            ("roles:list", "List Roles", "Roles", "View list of roles"),
            ("roles:create", "Create Roles", "Roles", "Create new roles"),
            ("roles:edit", "Edit Roles", "Roles", "Edit existing roles"),
            ("roles:delete", "Delete Roles", "Roles", "Delete roles"),
            ("roles:assign", "Assign Permissions", "Roles", "Assign permissions to roles"),
            ("permissions:list", "List Permissions", "Permissions", "View permission catalog"),
            ("permissions:assign", "Assign Permissions", "Permissions", "Assign permissions to roles"),
            ("audit:view", "View Audit Log", "Audit", "View audit log entries"),
            ("admin:dashboard", "View Dashboard", "Admin", "View admin dashboard"),
            ("admin:settings", "System Settings", "Admin", "Manage system settings"),
            ("profile:own:view", "View Own Profile", "Profile", "View own profile"),
            ("profile:own:edit", "Edit Own Profile", "Profile", "Edit own profile"),
            ("profile:own:password", "Change Own Password", "Profile", "Change own password"),
            ("page-a:view", "View Page A", "Pages", "Access to page A"),
            ("page-b:view", "View Page B", "Pages", "Access to page B"),
            ("page-c:view", "View Page C", "Pages", "Access to page C"),
            ("page-public:view", "View Public Page", "Public", "Access to public welcome page"),
        };

        foreach (var (code, name, module, description) in permissionDefs)
        {
            if (!await context.Permissions.AnyAsync(p => p.Code == code))
            {
                context.Permissions.Add(Permission.Create(code, name, module, description));
            }
        }
        await context.SaveChangesAsync();

        var roleDefs = new List<(string Name, string Description, bool IsSystem)>
        {
            ("SuperAdmin", "Full system access", true),
            ("Admin", "Administrative access", true),
            ("user-tipo-a", "Access to page A", false),
            ("user-tipo-b", "Access to page B", false),
            ("user-tipo-c", "Access to page C", false),
            ("public", "Public role for OAuth users", true),
        };

        foreach (var (name, description, isSystem) in roleDefs)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == name))
            {
                context.Roles.Add(Role.Create(name, description, isSystem));
            }
        }
        await context.SaveChangesAsync();

        var superAdminRole = await context.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
        var roleA = await context.Roles.FirstAsync(r => r.Name == "user-tipo-a");
        var roleB = await context.Roles.FirstAsync(r => r.Name == "user-tipo-b");
        var roleC = await context.Roles.FirstAsync(r => r.Name == "user-tipo-c");
        var publicRole = await context.Roles.FirstAsync(r => r.Name == "public");

        var allPerms = await context.Permissions.ToListAsync();

        // SuperAdmin gets all permissions
        foreach (var p in allPerms)
        {
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == superAdminRole.Id && rp.PermissionId == p.Id))
            {
                context.RolePermissions.Add(RolePermission.Create(superAdminRole.Id, p.Id));
            }
        }

        // Admin gets specific permissions
        foreach (var p in allPerms.Where(p => p.Code.StartsWith("users:")
            || p.Code.StartsWith("roles:") || p.Code.StartsWith("permissions:")
            || p.Code.StartsWith("audit:") || p.Code.StartsWith("admin:")
            || p.Code.StartsWith("profile:")))
        {
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == p.Id))
            {
                context.RolePermissions.Add(RolePermission.Create(adminRole.Id, p.Id));
            }
        }

        // Role-specific permissions (page + profile)
        var aCodes = new[] { "page-a:view", "profile:own:view", "profile:own:edit", "profile:own:password" };
        var bCodes = new[] { "page-b:view", "profile:own:view", "profile:own:edit", "profile:own:password" };
        var cCodes = new[] { "page-c:view", "profile:own:view", "profile:own:edit", "profile:own:password" };

        await EnsureRolePermissionsAsync(context, roleA.Id, aCodes);
        await EnsureRolePermissionsAsync(context, roleB.Id, bCodes);
        await EnsureRolePermissionsAsync(context, roleC.Id, cCodes);

        // Public role gets only page-public:view
        await EnsureRolePermissionsAsync(context, publicRole.Id, new[] { "page-public:view" });

        await context.SaveChangesAsync();

        // Admin user: always ensure exists
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@sistema.local");
        if (adminUser == null)
        {
            adminUser = User.Create(
                "admin@sistema.local",
                "Admin",
                "Usuario",
                hasher.HashPassword("admin"),
                null);

            adminUser.ConfirmEmail();
            adminUser.ForcePasswordChange();
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        // Ensure SuperAdmin role is assigned to admin user
        if (!await context.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == superAdminRole.Id))
        {
            context.UserRoles.Add(UserRole.Create(adminUser.Id, superAdminRole.Id));
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Database seeding completed.");
    }

    private static async Task EnsureRolePermissionsAsync(AppDbContext context, Guid roleId, string[] permissionCodes)
    {
        var perms = await context.Permissions.Where(p => permissionCodes.Contains(p.Code)).ToListAsync();
        foreach (var perm in perms)
        {
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == perm.Id))
            {
                context.RolePermissions.Add(RolePermission.Create(roleId, perm.Id));
            }
        }
    }
}
