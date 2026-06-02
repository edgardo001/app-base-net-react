using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Common.Interfaces;
using UserManagement.Domain.Entities;
using UserManagement.Infrastructure.Persistence;

namespace UserManagement.Infrastructure.Services;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        await context.Database.MigrateAsync();

        if (await context.Roles.AnyAsync()) return;

        logger.LogInformation("Seeding database...");

        // Permissions
        var permissions = new List<Permission>
        {
            Permission.Create("users:list", "List Users", "Users", "View list of users"),
            Permission.Create("users:create", "Create Users", "Users", "Create new users"),
            Permission.Create("users:edit", "Edit Users", "Users", "Edit existing users"),
            Permission.Create("users:delete", "Delete Users", "Users", "Soft delete users"),
            Permission.Create("users:activate", "Activate/Deactivate Users", "Users", "Toggle user active status"),
            Permission.Create("users:reset-password", "Reset User Password", "Users", "Reset another user's password"),
            Permission.Create("users:revoke-tokens", "Revoke User Tokens", "Users", "Revoke sessions for a user"),
            Permission.Create("roles:list", "List Roles", "Roles", "View list of roles"),
            Permission.Create("roles:create", "Create Roles", "Roles", "Create new roles"),
            Permission.Create("roles:edit", "Edit Roles", "Roles", "Edit existing roles"),
            Permission.Create("roles:delete", "Delete Roles", "Roles", "Delete roles"),
            Permission.Create("roles:assign", "Assign Permissions", "Roles", "Assign permissions to roles"),
            Permission.Create("permissions:list", "List Permissions", "Permissions", "View permission catalog"),
            Permission.Create("permissions:assign", "Assign Permissions", "Permissions", "Assign permissions to roles"),
            Permission.Create("audit:view", "View Audit Log", "Audit", "View audit log entries"),
            Permission.Create("admin:dashboard", "View Dashboard", "Admin", "View admin dashboard"),
            Permission.Create("admin:settings", "System Settings", "Admin", "Manage system settings"),
            Permission.Create("profile:own:view", "View Own Profile", "Profile", "View own profile"),
            Permission.Create("profile:own:edit", "Edit Own Profile", "Profile", "Edit own profile"),
            Permission.Create("profile:own:password", "Change Own Password", "Profile", "Change own password"),
            Permission.Create("page-a:view", "View Page A", "Pages", "Access to page A"),
            Permission.Create("page-b:view", "View Page B", "Pages", "Access to page B"),
            Permission.Create("page-c:view", "View Page C", "Pages", "Access to page C"),
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();

        // Roles
        var superAdminRole = Role.Create("SuperAdmin", "Full system access", isSystem: true);
        var adminRole = Role.Create("Admin", "Administrative access", isSystem: true);
        var roleA = Role.Create("user-tipo-a", "Access to page A");
        var roleB = Role.Create("user-tipo-b", "Access to page B");
        var roleC = Role.Create("user-tipo-c", "Access to page C");

        context.Roles.AddRange(superAdminRole, adminRole, roleA, roleB, roleC);
        await context.SaveChangesAsync();

        // RolePermissions - SuperAdmin gets all
        foreach (var p in permissions)
            context.RolePermissions.Add(RolePermission.Create(superAdminRole.Id, p.Id));

        // Admin gets specific permissions
        var adminPerms = permissions.Where(p => p.Code.StartsWith("users:")
            || p.Code.StartsWith("roles:") || p.Code.StartsWith("permissions:")
            || p.Code.StartsWith("audit:") || p.Code.StartsWith("admin:")
            || p.Code.StartsWith("profile:"));
        foreach (var p in adminPerms)
            context.RolePermissions.Add(RolePermission.Create(adminRole.Id, p.Id));

        // Role-specific permissions
        var permA = permissions.First(p => p.Code == "page-a:view");
        var permB = permissions.First(p => p.Code == "page-b:view");
        var permC = permissions.First(p => p.Code == "page-c:view");
        var profileView = permissions.First(p => p.Code == "profile:own:view");
        var profileEdit = permissions.First(p => p.Code == "profile:own:edit");
        var profilePwd = permissions.First(p => p.Code == "profile:own:password");

        context.RolePermissions.Add(RolePermission.Create(roleA.Id, permA.Id));
        context.RolePermissions.Add(RolePermission.Create(roleA.Id, profileView.Id));
        context.RolePermissions.Add(RolePermission.Create(roleA.Id, profileEdit.Id));
        context.RolePermissions.Add(RolePermission.Create(roleA.Id, profilePwd.Id));

        context.RolePermissions.Add(RolePermission.Create(roleB.Id, permB.Id));
        context.RolePermissions.Add(RolePermission.Create(roleB.Id, profileView.Id));
        context.RolePermissions.Add(RolePermission.Create(roleB.Id, profileEdit.Id));
        context.RolePermissions.Add(RolePermission.Create(roleB.Id, profilePwd.Id));

        context.RolePermissions.Add(RolePermission.Create(roleC.Id, permC.Id));
        context.RolePermissions.Add(RolePermission.Create(roleC.Id, profileView.Id));
        context.RolePermissions.Add(RolePermission.Create(roleC.Id, profileEdit.Id));
        context.RolePermissions.Add(RolePermission.Create(roleC.Id, profilePwd.Id));

        await context.SaveChangesAsync();

        // Admin user
        var adminUser = User.Create(
            "admin",
            "Admin",
            "Usuario",
            hasher.HashPassword("admin"),
            null);

        adminUser.ConfirmEmail();
        adminUser.ForcePasswordChange();
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        context.UserRoles.Add(UserRole.Create(adminUser.Id, superAdminRole.Id));
        await context.SaveChangesAsync();

        logger.LogInformation("Database seeding completed.");
    }
}
