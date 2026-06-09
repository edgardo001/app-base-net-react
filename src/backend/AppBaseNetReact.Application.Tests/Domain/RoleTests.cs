using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class RoleTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var role = Role.Create("Admin", "Administrator role");

        role.Name.Should().Be("Admin");
        role.NormalizedName.Should().Be("ADMIN");
        role.Description.Should().Be("Administrator role");
        role.IsSystem.Should().BeFalse();
        role.Id.Should().NotBe(Guid.Empty);
        role.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithIsSystem_SetsFlag()
    {
        var role = Role.Create("SuperAdmin", "Super admin", isSystem: true);

        role.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void Create_WithCreatedBy_SetsAuditField()
    {
        var userId = Guid.NewGuid();
        var role = Role.Create("Admin", "desc", createdBy: userId);

        role.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        var role = Role.Create("OldName", "Old desc");

        role.Update("NewName", "New desc");

        role.Name.Should().Be("NewName");
        role.NormalizedName.Should().Be("NEWNAME");
        role.Description.Should().Be("New desc");
        role.UpdatedAt.Should().NotBeNull();
        role.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_WithUpdatedBy_SetsAuditField()
    {
        var role = Role.Create("Admin", "desc");
        var userId = Guid.NewGuid();

        role.Update("NewName", "New desc", userId);

        role.UpdatedBy.Should().Be(userId);
    }

    [Fact]
    public void Role_HasEmptyCollections()
    {
        var role = Role.Create("Admin", "desc");

        role.UserRoles.Should().BeEmpty();
        role.RolePermissions.Should().BeEmpty();
    }
}
