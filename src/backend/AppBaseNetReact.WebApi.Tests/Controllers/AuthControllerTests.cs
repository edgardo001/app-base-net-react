using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<IPasswordPolicyService> _passwordPolicy = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILogger<EmailService>> _logger = new();
    private readonly EmailRenderer _renderer = new();
    private readonly EmailOptions _emailOptions = new()
    {
        Templates = new Dictionary<string, EmailTemplateConfig>
        {
            ["PasswordReset"] = new() { Subject = "Reset", TemplateFile = "password-reset.html" },
            ["PasswordChanged"] = new() { Subject = "Changed", TemplateFile = "password-changed.html" },
            ["Welcome"] = new() { Subject = "Welcome", TemplateFile = "welcome.html" },
            ["AccountLocked"] = new() { Subject = "Locked", TemplateFile = "account-locked.html" }
        }
    };

    private readonly AuthController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public AuthControllerTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _passwordPolicy.Setup(x => x.MaxFailedAccessAttempts).Returns(5);
        _passwordPolicy.Setup(x => x.DefaultLockoutMinutes).Returns(15);
        _passwordPolicy.Setup(x => x.Validate(It.IsAny<string>())).Returns((true, ""));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendUrl"] = "http://localhost:5173"
            })
            .Build();

        _controller = new AuthController(
            _jwt.Object,
            _hasher.Object,
            _uow.Object,
            clock.Object,
            _audit.Object,
            _passwordPolicy.Object,
            _email.Object,
            _renderer,
            Options.Create(_emailOptions),
            config);

        var claims = new[] { new Claim("sub", _userId.ToString()) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
                Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
            }
        };
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_SendsEmailAndReturnsGenericMessage()
    {
        var user = User.Create("test@test.com", "hash", "Test", "User", Guid.NewGuid());
        _uow.Setup(x => x.Users.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.ForgotPassword(
            new ForgotPasswordRequest("test@test.com"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeNull();
        _email.Verify(x => x.SendEmailAsync(
            user.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithUnregisteredEmail_ReturnsGenericMessage()
    {
        _uow.Setup(x => x.Users.GetByEmailAsync("unknown@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.ForgotPassword(
            new ForgotPasswordRequest("unknown@test.com"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeNull();
        _email.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_DoesNotReturnTempPassword()
    {
        var user = User.Create("test@test.com", "hash", "Test", "User", Guid.NewGuid());
        _uow.Setup(x => x.Users.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.ForgotPassword(
            new ForgotPasswordRequest("test@test.com"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ConfirmsAndSendsWelcome()
    {
        var user = User.Create("test@test.com", "hash", "Test", "User", Guid.NewGuid());
        user.SetEmailConfirmationToken("valid-token", DateTime.UtcNow.AddHours(24));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.ConfirmEmail(
            new ConfirmEmailRequest("valid-token"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        _email.Verify(x => x.SendEmailAsync(
            user.Email, "Welcome", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WithExpiredToken_ReturnsBadRequest()
    {
        var user = User.Create("test@test.com", "hash", "Test", "User", Guid.NewGuid());
        user.SetEmailConfirmationToken("expired-token", DateTime.UtcNow.AddHours(-1));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("expired-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.ConfirmEmail(
            new ConfirmEmailRequest("expired-token"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        var response = badRequest!.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsBadRequest()
    {
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.ConfirmEmail(
            new ConfirmEmailRequest("invalid"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
