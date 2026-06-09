using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class UserRoleTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var userRole = UserRole.Create(userId, roleId);

        userRole.UserId.Should().Be(userId);
        userRole.RoleId.Should().Be(roleId);
    }

    [Fact]
    public void Create_DifferentInstances_HaveDifferentKeys()
    {
        var ur1 = UserRole.Create(Guid.NewGuid(), Guid.NewGuid());
        var ur2 = UserRole.Create(Guid.NewGuid(), Guid.NewGuid());

        ur1.Should().NotBeSameAs(ur2);
    }
}
