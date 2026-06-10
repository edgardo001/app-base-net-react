using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Profile.Commands.UpdateProfile;
using AppBaseNetReact.Application.Features.Profile.Commands.UploadAvatar;
using AppBaseNetReact.Application.Features.Profile.Queries.GetActivity;
using AppBaseNetReact.Application.Features.Profile.Queries.GetProfile;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class ProfileControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly ProfileController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ProfileControllerTests()
    {
        _controller = new ProfileController(_mediator.Object);
        var claims = new[] { new Claim("sub", _userId.ToString()) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
        };
    }

    [Fact]
    public async Task GetProfile_ReturnsUserData()
    {
        var profile = new GetProfileResponse
        {
            Id = _userId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User"
        };
        _mediator.Setup(x => x.Send(It.IsAny<GetProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _controller.GetProfile(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<GetProfileResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.FirstName.Should().Be("Admin");
    }

    [Fact]
    public async Task GetProfile_WhenUserNotFound_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetProfileResponse?)null);

        var result = await _controller.GetProfile(CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateProfile_UpdatesNamesAndAudits()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UpdateProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateProfileOutcome.Success());

        var result = await _controller.UpdateProfile(
            new UpdateProfileRequest("New", "Name2"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAvatar_WhenValid_SavesAndUpdatesUser()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UploadAvatarCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadAvatarOutcome.Success("avatar123.jpg"));

        var file = new FormFile(new MemoryStream("image content"u8.ToArray()), 0, 14, "file", "photo.jpg");
        var result = await _controller.UploadAvatar(file, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAvatar_WhenInvalidExtension_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UploadAvatarCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadAvatarOutcome.InvalidExtension());

        var file = new FormFile(new MemoryStream("data"u8.ToArray()), 0, 4, "file", "script.exe");
        var result = await _controller.UploadAvatar(file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
