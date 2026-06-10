using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Roles.Commands.CreateRole;
using AppBaseNetReact.Application.Features.Roles.Commands.UpdateRole;
using AppBaseNetReact.Application.Features.Roles.Commands.DeleteRole;
using AppBaseNetReact.Application.Features.Roles.Commands.UpdatePermissions;
using AppBaseNetReact.Application.Features.Roles.Queries.GetRoles;
using AppBaseNetReact.Application.Features.Roles.Queries.GetRole;
using AppBaseNetReact.Application.Features.Roles.Queries.GetUsersByRole;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class RolesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _controller = new RolesController(_mediator.Object);
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
        _mediator.Setup(x => x.Send(It.IsAny<GetRolesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetRolesResponse
            {
                Items = new List<RoleListItemDto>
                {
                    new() { Id = Guid.NewGuid(), Name = "Admin", Description = "Administrator", IsSystem = true, CreatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Name = "User", Description = "Regular user", IsSystem = false, CreatedAt = DateTime.UtcNow }
                }
            });

        var result = await _controller.GetRoles(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<GetRolesResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRole_WhenExists_ReturnsOk()
    {
        var roleId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.Is<GetRoleQuery>(q => q.RoleId == roleId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetRoleResponse
            {
                Id = roleId,
                Name = "Admin",
                Description = "Administrator",
                IsSystem = true,
                CreatedAt = DateTime.UtcNow,
                Permissions = new List<RolePermissionDto>()
            });

        var result = await _controller.GetRole(roleId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRole_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetRoleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetRoleResponse?)null);

        var result = await _controller.GetRole(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateRole_WithValidRequest_ReturnsCreatedAtAction()
    {
        var roleId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.IsAny<CreateRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateRoleOutcome(CreateRoleResult.Success(roleId, "NewRole")));

        var result = await _controller.CreateRole(
            new CreateRoleRequest("NewRole", "A new role"), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
    }

    [Fact]
    public async Task CreateRole_WhenNameExists_ReturnsConflict()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CreateRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateRoleOutcome(CreateRoleResult.DuplicateName()));

        var result = await _controller.CreateRole(
            new CreateRoleRequest("Admin", "Duplicate"), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task UpdateRole_WhenExists_ReturnsOk()
    {
        var roleId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.IsAny<UpdateRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateRoleOutcome(UpdateRoleResult.Success()));

        var result = await _controller.UpdateRole(roleId,
            new UpdateRoleRequest("NewName", "New Desc"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateRole_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UpdateRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateRoleOutcome(UpdateRoleResult.NotFound()));

        var result = await _controller.UpdateRole(Guid.NewGuid(),
            new UpdateRoleRequest("Name", "Desc"), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateRole_WhenSystemRole_ReturnsUnprocessableEntity()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UpdateRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateRoleOutcome(UpdateRoleResult.CannotModifySystemRole()));

        var result = await _controller.UpdateRole(Guid.NewGuid(),
            new UpdateRoleRequest("NewName", "New Desc"), CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task DeleteRole_WhenExists_ReturnsOk()
    {
        var roleId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.IsAny<DeleteRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteRoleOutcome(DeleteRoleResult.Success()));

        var result = await _controller.DeleteRole(roleId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteRole_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<DeleteRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteRoleOutcome(DeleteRoleResult.NotFound()));

        var result = await _controller.DeleteRole(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteRole_WhenSystemRole_ReturnsUnprocessableEntity()
    {
        _mediator.Setup(x => x.Send(It.IsAny<DeleteRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteRoleOutcome(DeleteRoleResult.CannotDeleteSystemRole()));

        var result = await _controller.DeleteRole(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdatePermissions_WhenExists_ReturnsOk()
    {
        var roleId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.IsAny<UpdatePermissionsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdatePermissionsOutcome(UpdatePermissionsResult.Success()));

        var permId = Guid.NewGuid();
        var result = await _controller.UpdatePermissions(roleId,
            new UpdatePermissionsRequest(new List<PermissionAssignment>
            {
                new(permId, true)
            }), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdatePermissions_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UpdatePermissionsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdatePermissionsOutcome(UpdatePermissionsResult.NotFound()));

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
        _mediator.Setup(x => x.Send(It.Is<GetUsersByRoleQuery>(q => q.RoleId == roleId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUsersByRoleResponse
            {
                Users = new List<UserByRoleDto>
                {
                    new() { Id = Guid.NewGuid(), Email = "a@test.com", FirstName = "Alice", LastName = "User", IsActive = true }
                }
            });

        var result = await _controller.GetUsersByRole(roleId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<GetUsersByRoleResponse>>().Subject;
        response.Data!.Users.Should().HaveCount(1);
        response.Data.Users[0].Email.Should().Be("a@test.com");
    }

    [Fact]
    public async Task GetUsersByRole_WhenRoleNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetUsersByRoleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUsersByRoleResponse?)null);

        var result = await _controller.GetUsersByRole(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUsersByRole_WhenNoUsers_ReturnsEmptyList()
    {
        var roleId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.Is<GetUsersByRoleQuery>(q => q.RoleId == roleId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUsersByRoleResponse
            {
                Users = new List<UserByRoleDto>()
            });

        var result = await _controller.GetUsersByRole(roleId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<GetUsersByRoleResponse>>().Subject;
        response.Data!.Users.Should().BeEmpty();
    }
}
