using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class RolesControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _controller = new RolesController(_uow.Object, _audit.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Scheme = "https", Host = new HostString("app.example.com") }
            }
        };
    }

    [Fact]
    public async Task GetRoles_ReturnsOkWithRoles()
    {
        var roles = new List<Role>
        {
            Role.Create("Admin", "Administrator", true),
            Role.Create("User", "Regular user")
        };
        _uow.Setup(x => x.Roles.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var result = await _controller.GetRoles(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<RoleDetailDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRole_WhenExists_ReturnsOkWithPermissions()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "Administrator", true);
        role.RolePermissions.Add(RolePermission.Create(roleId, Guid.NewGuid(), true));

        _uow.Setup(x => x.Roles.GetByIdWithPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await _controller.GetRole(roleId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRole_WhenNotExists_ReturnsNotFound()
    {
        _uow.Setup(x => x.Roles.GetByIdWithPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _controller.GetRole(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateRole_WithValidRequest_ReturnsCreatedAtAction()
    {
        _uow.Setup(x => x.Roles.GetByNameAsync("NewRole", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _controller.CreateRole(
            new CreateRoleRequest("NewRole", "A new role"), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        _uow.Verify(x => x.Roles.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(x => x.LogAsync(
            "RoleCreated", "Role", It.IsAny<string>(),
            null, null, It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRole_WhenNameExists_ReturnsConflict()
    {
        var existing = Role.Create("Admin", "Existing");
        _uow.Setup(x => x.Roles.GetByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _controller.CreateRole(
            new CreateRoleRequest("Admin", "Duplicate"), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
        _uow.Verify(x => x.Roles.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRole_WhenExists_ReturnsOk()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("OldName", "Old Desc");
        _uow.Setup(x => x.Roles.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await _controller.UpdateRole(roleId,
            new UpdateRoleRequest("NewName", "New Desc"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(x => x.LogAsync(
            "RoleUpdated", "Role", It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRole_WhenNotExists_ReturnsNotFound()
    {
        _uow.Setup(x => x.Roles.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _controller.UpdateRole(Guid.NewGuid(),
            new UpdateRoleRequest("Name", "Desc"), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateRole_WhenSystemRole_ReturnsUnprocessableEntity()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("SuperAdmin", "System", isSystem: true);
        _uow.Setup(x => x.Roles.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await _controller.UpdateRole(roleId,
            new UpdateRoleRequest("NewName", "New Desc"), CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task DeleteRole_WhenExists_ReturnsOk()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("ToDelete", "desc");
        _uow.Setup(x => x.Roles.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await _controller.DeleteRole(roleId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _uow.Verify(x => x.Roles.DeleteAsync(role, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(x => x.LogAsync(
            "RoleDeleted", "Role", It.IsAny<string>(),
            null, null, It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRole_WhenNotExists_ReturnsNotFound()
    {
        _uow.Setup(x => x.Roles.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _controller.DeleteRole(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteRole_WhenSystemRole_ReturnsUnprocessableEntity()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("SuperAdmin", "System", isSystem: true);
        _uow.Setup(x => x.Roles.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await _controller.DeleteRole(roleId, CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdatePermissions_WhenExists_ReturnsOk()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "desc");
        _uow.Setup(x => x.Roles.GetByIdWithPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var permId = Guid.NewGuid();
        var result = await _controller.UpdatePermissions(roleId,
            new UpdatePermissionsRequest(new List<PermissionAssignment>
            {
                new(permId, true)
            }), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(x => x.LogAsync(
            "RolePermissionsUpdated", "Role", It.IsAny<string>(),
            null, It.IsAny<string>(),
            It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePermissions_WhenNotExists_ReturnsNotFound()
    {
        _uow.Setup(x => x.Roles.GetByIdWithPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _controller.UpdatePermissions(Guid.NewGuid(),
            new UpdatePermissionsRequest(new List<PermissionAssignment>
            {
                new(Guid.NewGuid(), true)
            }), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUsersByRole_WhenExists_ReturnsOkWithUsers()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "desc");
        _uow.Setup(x => x.Roles.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var users = new List<AppBaseNetReact.Domain.Entities.User>
        {
            AppBaseNetReact.Domain.Entities.User.Create("a@test.com", "A", "User", "hash")
        };
        _uow.Setup(x => x.Users.GetUsersByRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _controller.GetUsersByRole(roleId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<UserByRoleDto>>>().Subject;
        response.Data.Should().HaveCount(1);
        response.Data![0].Email.Should().Be("a@test.com");
    }

    [Fact]
    public async Task GetUsersByRole_WhenRoleNotExists_ReturnsNotFound()
    {
        _uow.Setup(x => x.Roles.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _controller.GetUsersByRole(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUsersByRole_WhenNoUsers_ReturnsEmptyList()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "desc");
        _uow.Setup(x => x.Roles.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _uow.Setup(x => x.Users.GetUsersByRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AppBaseNetReact.Domain.Entities.User>());

        var result = await _controller.GetUsersByRole(roleId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<UserByRoleDto>>>().Subject;
        response.Data.Should().BeEmpty();
    }
}
