using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AppBaseNetReact.Application.Features.Permissions.Queries.GetPermissions;
using AppBaseNetReact.Application.Features.Permissions.Queries.GetModules;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class PermissionsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly PermissionsController _controller;

    public PermissionsControllerTests()
    {
        _controller = new PermissionsController(_mediator.Object);
    }

    [Fact]
    public async Task GetPermissions_ReturnsOkWithPermissions()
    {
        var response = new GetPermissionsResponse
        {
            Items = new List<PermissionItemDto>
            {
                new() { Id = Guid.NewGuid(), Code = "users.read", Name = "Read Users", Module = "Users", Description = "Read user data" },
                new() { Id = Guid.NewGuid(), Code = "users.write", Name = "Write Users", Module = "Users", Description = "Write user data" },
                new() { Id = Guid.NewGuid(), Code = "roles.read", Name = "Read Roles", Module = "Roles", Description = "Read role data" }
            }
        };
        _mediator.Setup(x => x.Send(It.IsAny<GetPermissionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetPermissions(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = ok.Value.Should().BeOfType<ApiResponse<GetPermissionsResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetModules_ReturnsOkGroupedByModule()
    {
        var response = new GetModulesResponse
        {
            Modules = new List<ModuleGroupDto>
            {
                new()
                {
                    Module = "Users",
                    Permissions = new List<ModulePermissionDto>
                    {
                        new() { Id = Guid.NewGuid(), Code = "users.read", Name = "Read Users", Description = "Read user data" },
                        new() { Id = Guid.NewGuid(), Code = "users.write", Name = "Write Users", Description = "Write user data" }
                    }
                },
                new()
                {
                    Module = "Roles",
                    Permissions = new List<ModulePermissionDto>
                    {
                        new() { Id = Guid.NewGuid(), Code = "roles.read", Name = "Read Roles", Description = "Read role data" }
                    }
                }
            }
        };
        _mediator.Setup(x => x.Send(It.IsAny<GetModulesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetModules(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = ok.Value.Should().BeOfType<ApiResponse<GetModulesResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Modules.Should().HaveCount(2);
    }
}
