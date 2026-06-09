using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class RolePermissionTests
{
    [Fact]
    public void Create_DefaultGranted_ReturnsTrue()
    {
        var rp = RolePermission.Create(Guid.NewGuid(), Guid.NewGuid());

        rp.Granted.Should().BeTrue();
    }

    [Fact]
    public void Create_WithGrantedFalse_SetsFalse()
    {
        var rp = RolePermission.Create(Guid.NewGuid(), Guid.NewGuid(), granted: false);

        rp.Granted.Should().BeFalse();
    }

    [Fact]
    public void SetGranted_TogglesValue()
    {
        var rp = RolePermission.Create(Guid.NewGuid(), Guid.NewGuid(), granted: true);

        rp.SetGranted(false);

        rp.Granted.Should().BeFalse();
    }

    [Fact]
    public void Create_SetsIds()
    {
        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();

        var rp = RolePermission.Create(roleId, permId);

        rp.RoleId.Should().Be(roleId);
        rp.PermissionId.Should().Be(permId);
    }
}
