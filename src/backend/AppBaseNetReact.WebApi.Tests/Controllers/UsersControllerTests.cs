using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Users.Commands.AdminResetPassword;
using AppBaseNetReact.Application.Features.Users.Commands.CreateUser;
using AppBaseNetReact.Application.Features.Users.Commands.DeleteUser;
using AppBaseNetReact.Application.Features.Users.Commands.RevokeTokens;
using AppBaseNetReact.Application.Features.Users.Commands.ToggleActive;
using AppBaseNetReact.Application.Features.Users.Commands.UpdateUser;
using AppBaseNetReact.Application.Features.Users.Commands.UploadAvatar;
using AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail;
using AppBaseNetReact.Application.Features.Users.Queries.GetAvatar;
using AppBaseNetReact.Application.Features.Users.Queries.GetUser;
using AppBaseNetReact.Application.Features.Users.Queries.GetUsers;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FrontendUrl"] = "http://localhost:5173" })
            .Build();

        _controller = new UsersController(_mediator.Object, configuration);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Scheme = "https", Host = new HostString("app.example.com") }
            }
        };
    }

    // ── GetUsers ──

    [Fact]
    public async Task GetUsers_ReturnsOkWithPagedResponse()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUsersResponse
            {
                Items = [new() { Id = Guid.NewGuid(), Email = "a@test.com", FirstName = "A", LastName = "B" }],
                TotalCount = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            });

        var result = await _controller.GetUsers(page: 1, pageSize: 10, ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<UserDto>>().Subject;
        response.Items.Should().HaveCount(1);
        response.TotalCount.Should().Be(1);
    }

    // ── GetUser ──

    [Fact]
    public async Task GetUser_WhenExists_ReturnsOkWithUserDetail()
    {
        var userId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.Is<GetUserQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserResponse
            {
                Id = userId,
                Email = "a@test.com",
                FirstName = "A",
                LastName = "B"
            });

        var result = await _controller.GetUser(userId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<GetUserResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Email.Should().Be("a@test.com");
    }

    [Fact]
    public async Task GetUser_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUserResponse?)null);

        var result = await _controller.GetUser(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── CreateUser ──

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedAtAction()
    {
        var userId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserOutcome(CreateUserResult.Success(userId, "new@test.com")));

        var result = await _controller.CreateUser(
            new CreateUserRequest("new@test.com", "Test", "User", null), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        _mediator.Verify(x => x.Send(
            It.Is<CreateUserCommand>(c => c.Email == "new@test.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Returns409()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserOutcome(CreateUserResult.DuplicateEmail()));

        var result = await _controller.CreateUser(
            new CreateUserRequest("taken@test.com", "Test", "User", null), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── ResendOnboardingEmail ──

    [Fact]
    public async Task ResendOnboardingEmail_WhenHandlerSucceeds_Returns200()
    {
        var userId = Guid.NewGuid();
        _mediator.Setup(x => x.Send(It.IsAny<ResendOnboardingEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendOnboardingEmailOutcome(ResendOnboardingEmailResult.Success()));

        var result = await _controller.ResendOnboardingEmail(userId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(x => x.Send(
            It.Is<ResendOnboardingEmailCommand>(c => c.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendOnboardingEmail_WhenUserNotFound_Returns404()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ResendOnboardingEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendOnboardingEmailOutcome(
                ResendOnboardingEmailResult.Fail(ResendOnboardingErrorCode.UserNotFound, "User not found")));

        var result = await _controller.ResendOnboardingEmail(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ResendOnboardingEmail_WhenAlreadyConfirmed_Returns409()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ResendOnboardingEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendOnboardingEmailOutcome(
                ResendOnboardingEmailResult.Fail(ResendOnboardingErrorCode.AlreadyConfirmed, "Already confirmed")));

        var result = await _controller.ResendOnboardingEmail(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── UpdateUser ──

    [Fact]
    public async Task UpdateUser_WhenExists_ReturnsOk()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateUserOutcome(UpdateUserResult.Success()));

        var result = await _controller.UpdateUser(Guid.NewGuid(),
            new UpdateUserRequest("NewFirst", "NewLast", null), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateUser_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateUserOutcome(UpdateUserResult.UserNotFound()));

        var result = await _controller.UpdateUser(Guid.NewGuid(),
            new UpdateUserRequest("First", "Last", null), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── ToggleActive ──

    [Fact]
    public async Task ToggleActive_WhenExists_ReturnsOk()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ToggleActiveCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToggleActiveOutcome(ToggleActiveResult.Success(false)));

        var result = await _controller.ToggleActive(Guid.NewGuid(),
            new ToggleActiveRequest(false), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ToggleActive_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ToggleActiveCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToggleActiveOutcome(ToggleActiveResult.UserNotFound()));

        var result = await _controller.ToggleActive(Guid.NewGuid(),
            new ToggleActiveRequest(true), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── ResetPassword ──

    [Fact]
    public async Task ResetPassword_WhenExists_ReturnsOk()
    {
        _mediator.Setup(x => x.Send(It.IsAny<AdminResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminResetPasswordOutcome(AdminResetPasswordResult.Success("TempPass123")));

        var result = await _controller.ResetPassword(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<AdminResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminResetPasswordOutcome(AdminResetPasswordResult.UserNotFound()));

        var result = await _controller.ResetPassword(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── RevokeTokens ──

    [Fact]
    public async Task RevokeTokens_ReturnsOk()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RevokeTokensCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevokeTokensOutcome(RevokeTokensResult.Success()));

        var result = await _controller.RevokeTokens(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ── DeleteUser ──

    [Fact]
    public async Task DeleteUser_WhenExists_ReturnsOk()
    {
        _mediator.Setup(x => x.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteUserOutcome(DeleteUserResult.Success()));

        var result = await _controller.DeleteUser(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteUser_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteUserOutcome(DeleteUserResult.UserNotFound()));

        var result = await _controller.DeleteUser(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteUser_WhenDeletingSelf_ReturnsBadRequest()
    {
        _mediator.Setup(x => x.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteUserOutcome(DeleteUserResult.CannotDeleteSelf()));

        var result = await _controller.DeleteUser(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── UploadAvatar ──

    [Fact]
    public async Task UploadAvatar_WhenValidFile_ReturnsOk()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UploadAvatarCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadAvatarOutcome(UploadAvatarResult.Success("avatar123.jpg")));

        var file = new FormFile(new MemoryStream("image content"u8.ToArray()), 0, 14, "file", "photo.jpg");
        var result = await _controller.UploadAvatar(Guid.NewGuid(), file, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UploadAvatar_WhenNotExists_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UploadAvatarCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadAvatarOutcome(UploadAvatarResult.UserNotFound()));

        var file = new FormFile(new MemoryStream("data"u8.ToArray()), 0, 4, "file", "photo.jpg");
        var result = await _controller.UploadAvatar(Guid.NewGuid(), file, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UploadAvatar_WhenInvalidExtension_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<UploadAvatarCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadAvatarOutcome(UploadAvatarResult.InvalidExtension(".jpg, .jpeg, .png, .webp")));

        var file = new FormFile(new MemoryStream("data"u8.ToArray()), 0, 4, "file", "script.exe");
        var result = await _controller.UploadAvatar(Guid.NewGuid(), file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GetAvatar ──

    [Fact]
    public async Task GetAvatar_WhenExists_ReturnsFile()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, "image bytes"u8.ToArray());
        try
        {
            _mediator.Setup(x => x.Send(It.IsAny<GetAvatarQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetAvatarOutcome(GetAvatarResult.Success(tempFile, "image/jpeg")));

            var result = await _controller.GetAvatar(Guid.NewGuid(), CancellationToken.None);

            result.Should().BeOfType<PhysicalFileResult>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetAvatar_WhenNoAvatar_ReturnsNotFound()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetAvatarQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAvatarOutcome(GetAvatarResult.NoAvatar()));

        var result = await _controller.GetAvatar(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
