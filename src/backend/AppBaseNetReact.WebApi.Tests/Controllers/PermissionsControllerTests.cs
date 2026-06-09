using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class PermissionsControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly PermissionsController _controller;

    public PermissionsControllerTests()
    {
        _controller = new PermissionsController(_uow.Object);
    }

    [Fact]
    public async Task GetPermissions_ReturnsOkWithPermissions()
    {
        var permissions = new List<Permission>
        {
            Permission.Create("users.read", "Read Users", "Users", "Read user data"),
            Permission.Create("users.write", "Write Users", "Users", "Write user data"),
            Permission.Create("roles.read", "Read Roles", "Roles", "Read role data")
        };
        _uow.Setup(x => x.Permissions.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var result = await _controller.GetPermissions(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetModules_ReturnsOkGroupedByModule()
    {
        var permissions = new List<Permission>
        {
            Permission.Create("users.read", "Read Users", "Users", "Read user data"),
            Permission.Create("users.write", "Write Users", "Users", "Write user data"),
            Permission.Create("roles.read", "Read Roles", "Roles", "Read role data")
        };
        _uow.Setup(x => x.Permissions.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var result = await _controller.GetModules(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
    }
}
