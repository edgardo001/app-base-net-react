using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class PermissionTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var permission = Permission.Create("users.read", "Read Users", "Users", "Read user data");

        permission.Code.Should().Be("users.read");
        permission.Name.Should().Be("Read Users");
        permission.Module.Should().Be("Users");
        permission.Description.Should().Be("Read user data");
        permission.Id.Should().NotBe(Guid.Empty);
        permission.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Permission_HasEmptyRolePermissions()
    {
        var permission = Permission.Create("test", "Test", "Module", "desc");

        permission.RolePermissions.Should().BeEmpty();
    }
}
