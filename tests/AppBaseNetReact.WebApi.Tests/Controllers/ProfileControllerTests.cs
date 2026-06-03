using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class ProfileControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly ProfileController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ProfileControllerTests()
    {
        _controller = new ProfileController(_uow.Object, _audit.Object);
        var claims = new[] { new Claim("sub", _userId.ToString()) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
        };
    }

    [Fact]
    public async Task GetProfile_ReturnsUserData()
    {
        var user = User.Create("admin@test.com", "hash", "Admin", "User", _userId);
        _uow.Setup(x => x.Users.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.GetProfile(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetProfile_WhenUserNotFound_ReturnsNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.GetProfile(CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateProfile_UpdatesNamesAndAudits()
    {
        var user = User.Create("admin@test.com", "hash", "Old", "Name", _userId);
        _uow.Setup(x => x.Users.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.UpdateProfile(
            new Application.Common.Validators.UpdateProfileRequest("New", "Name2"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("Name2");
        _audit.Verify(x => x.LogAsync(
            "ProfileUpdated", "User", user.Id.ToString(),
            It.IsAny<string?>(), It.IsAny<string?>(), _userId,
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
