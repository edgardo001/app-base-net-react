using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Auth.Commands.ChangePassword;
using AppBaseNetReact.Application.Features.Auth.Commands.ConfirmEmail;
using AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;
using AppBaseNetReact.Application.Features.Auth.Commands.Login;
using AppBaseNetReact.Application.Features.Auth.Commands.Logout;
using AppBaseNetReact.Application.Features.Auth.Commands.Refresh;
using AppBaseNetReact.Application.Features.Auth.Commands.ResetPassword;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediator = new();

    private readonly AuthController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public AuthControllerTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendUrl"] = "http://localhost:5173"
            })
            .Build();

        var emailOptions = Options.Create(new EmailOptions { ForgotPasswordEnabled = true });
        _controller = new AuthController(_mediator.Object, config, emailOptions);

        var claims = new[] { new Claim("sub", _userId.ToString()) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
                Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") },
                Request = { Scheme = "http", Host = new HostString("localhost:5173") }
            }
        };
    }

    [Fact]
    public async Task ForgotPassword_AlwaysReturnsGenericMessage()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ForgotPasswordOutcome(PasswordResult.Success()));

        var result = await _controller.ForgotPassword(
            new ForgotPasswordRequest("test@test.com"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeNull();
        _mediator.Verify(x => x.Send(
            It.Is<ForgotPasswordCommand>(c => c.Email == "test@test.com" && c.IpAddress == "127.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_SendsCommandEvenForUnknownEmail()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ForgotPasswordOutcome(PasswordResult.Success()));

        var result = await _controller.ForgotPassword(
            new ForgotPasswordRequest("unknown@test.com"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(x => x.Send(
            It.Is<ForgotPasswordCommand>(c => c.Email == "unknown@test.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WithSuccessOutcome_Returns200()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ConfirmEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmEmailOutcome(EmailConfirmationResult.Success()));

        var result = await _controller.ConfirmEmail(
            new ConfirmEmailRequest("valid-token"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
        _mediator.Verify(x => x.Send(
            It.Is<ConfirmEmailCommand>(c =>
                c.Token == "valid-token" &&
                c.LoginLink == "http://localhost:5173/login" &&
                c.IpAddress == "127.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WithExpiredToken_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ConfirmEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmEmailOutcome(
                EmailConfirmationResult.Fail(EmailErrorCode.ConfirmationTokenExpired, "Confirmation token has expired")));

        var result = await _controller.ConfirmEmail(
            new ConfirmEmailRequest("expired-token"), CancellationToken.None);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = bad.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ConfirmEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmEmailOutcome(
                EmailConfirmationResult.Fail(EmailErrorCode.InvalidConfirmationToken, "Invalid confirmation token")));

        var result = await _controller.ConfirmEmail(
            new ConfirmEmailRequest("invalid"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_WithSuccessOutcome_Returns200()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordOutcome(PasswordResult.Success()));

        var result = await _controller.ChangePassword(
            new ChangePasswordRequest("current", "new-pwd", "new-pwd"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        _mediator.Verify(x => x.Send(
            It.Is<ChangePasswordCommand>(c =>
                c.UserId == _userId &&
                c.CurrentPassword == "current" &&
                c.NewPassword == "new-pwd" &&
                c.IpAddress == "127.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.InvalidCurrentPassword, "Current password is incorrect")));

        var result = await _controller.ChangePassword(
            new ChangePasswordRequest("wrong", "new-pwd", "new-pwd"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_WithWeakNewPassword_Returns400WithMessage()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.WeakPassword, "Password too short")));

        var result = await _controller.ChangePassword(
            new ChangePasswordRequest("current", "weak", "weak"), CancellationToken.None);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = bad.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Message.Should().Be("Password too short");
    }

    [Fact]
    public async Task ChangePassword_WithUserNotFound_Returns404()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.UserNotFound, "User not found")));

        var result = await _controller.ChangePassword(
            new ChangePasswordRequest("current", "new-pwd", "new-pwd"), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangePassword_WithoutSubClaim_Returns401()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()),
                Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
            }
        };

        var result = await _controller.ChangePassword(
            new ChangePasswordRequest("current", "new-pwd", "new-pwd"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        _mediator.Verify(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPassword_WithSuccessOutcome_Returns200()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResetPasswordOutcome(PasswordResult.Success()));

        var result = await _controller.ResetPassword(
            new ResetPasswordRequest("valid-token", "new-pwd", "new-pwd"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        _mediator.Verify(x => x.Send(
            It.Is<ResetPasswordCommand>(c =>
                c.Token == "valid-token" &&
                c.NewPassword == "new-pwd" &&
                c.IpAddress == "127.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResetPasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.InvalidResetToken, "Invalid reset token")));

        var result = await _controller.ResetPassword(
            new ResetPasswordRequest("invalid", "new-pwd", "new-pwd"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResetPasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.ResetTokenExpired, "Reset token has expired")));

        var result = await _controller.ResetPassword(
            new ResetPasswordRequest("expired-token", "new-pwd", "new-pwd"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_WithWeakNewPassword_Returns400()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResetPasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.WeakPassword, "Password too short")));

        var result = await _controller.ResetPassword(
            new ResetPasswordRequest("valid-token", "weak", "weak"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static LoginResponse BuildLoginResponse(Guid userId = default, bool passwordExpired = false) =>
        new("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(15),
            userId == default ? Guid.NewGuid() : userId,
            "test@test.com", "Test", "User", null,
            new List<string>(), passwordExpired);

    [Fact]
    public async Task Login_WithSuccessfulOutcome_Returns200()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Success(), BuildLoginResponse()));

        var result = await _controller.Login(new LoginRequest("test@test.com", "plain"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        _mediator.Verify(x => x.Send(
            It.Is<LoginCommand>(c => c.Email == "test@test.com" && c.Password == "plain" && c.IpAddress == "127.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidCredentialsOutcome_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Fail(LoginErrorCode.InvalidCredentials, "Invalid email or password"), null));

        var result = await _controller.Login(new LoginRequest("test@test.com", "wrong"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithUnknownEmailOutcome_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Fail(LoginErrorCode.InvalidCredentials, "Invalid email or password"), null));

        var result = await _controller.Login(new LoginRequest("ghost@test.com", "any"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithDeactivatedAccountOutcome_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Fail(LoginErrorCode.AccountDeactivated, "Account is deactivated"), null));

        var result = await _controller.Login(new LoginRequest("test@test.com", "plain"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithLockedAccountOutcome_Returns423()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Fail(LoginErrorCode.AccountLocked, "Account is locked. Try again in 10 minutes.", 10), null));

        var result = await _controller.Login(new LoginRequest("test@test.com", "plain"), CancellationToken.None);

        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(423);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmailOutcome_Returns403()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Fail(LoginErrorCode.EmailNotConfirmed, "Email not confirmed. Check your inbox."), null));

        var result = await _controller.Login(new LoginRequest("test@test.com", "plain"), CancellationToken.None);

        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Login_PassesIpAddressAndUserAgentToHandler()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Success(), BuildLoginResponse()));

        await _controller.Login(new LoginRequest("test@test.com", "plain"), CancellationToken.None);

        _mediator.Verify(x => x.Send(
            It.Is<LoginCommand>(c => c.IpAddress == "127.0.0.1" && c.UserAgent != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithPasswordExpiredResponse_Returns200WithFlag()
    {
        _mediator.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginOutcome(LoginResult.Success(), BuildLoginResponse(passwordExpired: true)));

        var result = await _controller.Login(new LoginRequest("test@test.com", "plain"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithValidToken_Returns200AndRotatesTokens()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshOutcome(
                RefreshResult.Success(),
                new RefreshResponse("new-access", "new-refresh", DateTime.UtcNow.AddMinutes(15))));

        var result = await _controller.Refresh(new RefreshRequest("raw-refresh"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        _mediator.Verify(x => x.Send(
            It.Is<RefreshCommand>(c => c.RefreshToken == "raw-refresh"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithInvalidTokenFromHandler_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshOutcome(
                RefreshResult.Fail(RefreshErrorCode.InvalidToken, "Invalid refresh token"), null));

        var result = await _controller.Refresh(new RefreshRequest("ghost"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithCompromisedTokenFromHandler_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshOutcome(
                RefreshResult.Fail(RefreshErrorCode.TokenCompromised, "Token compromised. All sessions revoked."), null));

        var result = await _controller.Refresh(new RefreshRequest("raw"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithExpiredTokenFromHandler_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshOutcome(
                RefreshResult.Fail(RefreshErrorCode.TokenExpired, "Refresh token expired"), null));

        var result = await _controller.Refresh(new RefreshRequest("raw"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithMissingUserFromHandler_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshOutcome(
                RefreshResult.Fail(RefreshErrorCode.UserNotFoundOrInactive, "User not found or inactive"), null));

        var result = await _controller.Refresh(new RefreshRequest("raw"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithInactiveUserFromHandler_Returns401()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshOutcome(
                RefreshResult.Fail(RefreshErrorCode.UserNotFoundOrInactive, "User not found or inactive"), null));

        var result = await _controller.Refresh(new RefreshRequest("raw"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_SendsLogoutCommandAndReturns200()
    {
        var result = await _controller.Logout(new RefreshRequest("raw"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        _mediator.Verify(x => x.Send(
            It.Is<LogoutCommand>(c => c.RefreshToken == "raw"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
