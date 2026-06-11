using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;
using AppBaseNetReact.WebApi.Controllers;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class GoogleAuthControllerTests
{
    private readonly Mock<IGoogleAuthService> _googleAuth = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<GoogleAuthController>> _logger = new();
    private readonly GoogleAuthController _controller;

    public GoogleAuthControllerTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendUrl"] = "http://localhost:5173"
            })
            .Build();

        _googleAuth.Setup(x => x.GetAuthorizationUrl(It.IsAny<string>()))
            .Returns("https://accounts.google.com/o/oauth2/auth?client_id=xxx");

        _controller = new GoogleAuthController(_googleAuth.Object, _mediator.Object, config, _logger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") },
                Request = { Scheme = "http", Host = new HostString("localhost:5173") }
            }
        };
        _controller.Request.Headers.UserAgent = "Mozilla/5.0";
    }

    [Fact]
    public void Login_ReturnsRedirectToGoogle()
    {
        var result = _controller.Login();

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("https://accounts.google.com/o/oauth2/auth?client_id=xxx");
        redirect.Permanent.Should().BeFalse();
        _googleAuth.Verify(x => x.GetAuthorizationUrl(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Callback_WithValidCodeAndState_RedirectsToFrontendWithTokens()
    {
        var successOutcome = new GoogleLoginOutcome(
            GoogleLoginResult.Success(),
            "access-token-value",
            "refresh-token-value",
            DateTime.UtcNow.AddMinutes(15));

        _mediator.Setup(x => x.Send(
            It.Is<GoogleLoginCommand>(c => c.Code == "valid-code" && c.State == "valid-state"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(successOutcome);

        var result = await _controller.Callback("valid-code", "valid-state", CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Contain("/oauth-callback#");
        redirect.Url.Should().Contain("accessToken=access-token-value");
        redirect.Url.Should().Contain("refreshToken=refresh-token-value");
        redirect.Url.Should().Contain("expiresAt=");
        _mediator.Verify(x => x.Send(
            It.Is<GoogleLoginCommand>(c =>
                c.Code == "valid-code" &&
                c.State == "valid-state" &&
                c.IpAddress == "127.0.0.1" &&
                c.UserAgent == "Mozilla/5.0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Callback_WithMissingCode_RedirectsToLoginError()
    {
        var result = await _controller.Callback("", "valid-state", CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("http://localhost:5173/login?error=google_auth_failed");
        _mediator.Verify(x => x.Send(It.IsAny<GoogleLoginCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Callback_WithMissingState_RedirectsToLoginError()
    {
        var result = await _controller.Callback("valid-code", "", CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("http://localhost:5173/login?error=google_auth_failed");
        _mediator.Verify(x => x.Send(It.IsAny<GoogleLoginCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Callback_WithFailedOutcome_RedirectsToLoginError()
    {
        var failOutcome = new GoogleLoginOutcome(
            GoogleLoginResult.Fail(GoogleLoginErrorCode.AuthFailed, "Google authentication failed"));

        _mediator.Setup(x => x.Send(It.IsAny<GoogleLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failOutcome);

        var result = await _controller.Callback("invalid-code", "invalid-state", CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("http://localhost:5173/login?error=google_auth_failed");
    }

    [Fact]
    public async Task Callback_WithInvalidStateError_RedirectsToLoginError()
    {
        var failOutcome = new GoogleLoginOutcome(
            GoogleLoginResult.Fail(GoogleLoginErrorCode.InvalidState, "Invalid state parameter"));

        _mediator.Setup(x => x.Send(It.IsAny<GoogleLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failOutcome);

        var result = await _controller.Callback("code", "bad-state", CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("http://localhost:5173/login?error=google_auth_failed");
    }
}
