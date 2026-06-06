using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly EmailRenderer _renderer = new();
    private readonly EmailOptions _emailOptions = new()
    {
        Templates = new Dictionary<string, EmailTemplateConfig>
        {
            ["Welcome"] = new() { Subject = "Bienvenido", TemplateFile = "welcome.html" },
            ["EmailConfirmation"] = new() { Subject = "Confirma tu correo", TemplateFile = "email-confirmation.html" },
            ["TemporaryPassword"] = new() { Subject = "Contraseña temporal", TemplateFile = "temporary-password.html" }
        }
    };
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _uow.Setup(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _controller = new UsersController(
            _uow.Object, _hasher.Object, _email.Object, _renderer, Options.Create(_emailOptions));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Scheme = "https", Host = new HostString("app.example.com") }
            }
        };
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_PersistsAndSendsConfirmationEmail()
    {
        User? capturedUser = null;
        _uow.Setup(x => x.Users.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _uow.Setup(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        var result = await _controller.CreateUser(
            new CreateUserRequest("new@test.com", "Test", "User", "Password1!", null),
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        _uow.Verify(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        capturedUser.Should().NotBeNull();
        capturedUser!.EmailConfirmationToken.Should().NotBeNullOrEmpty();
        capturedUser.EmailConfirmed.Should().BeFalse();
        capturedUser.EmailConfirmationTokenExpires.Should().NotBeNull();
        capturedUser.EmailConfirmationTokenExpires!.Value
            .Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));

        _email.Verify(x => x.SendEmailAsync(
            "new@test.com", "Confirma tu correo", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_SendsEmailWithConfirmationLink()
    {
        string? capturedBody = null;
        _uow.Setup(x => x.Users.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _email.Setup(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        await _controller.CreateUser(
            new CreateUserRequest("new@test.com", "Test", "User", "Password1!", null),
            CancellationToken.None);

        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("http://localhost:5173/confirm-email?token=");
    }

    [Fact]
    public async Task CreateUser_UsesConfiguredFrontendBaseUrl()
    {
        var customOptions = new EmailOptions
        {
            FrontendBaseUrl = "https://app.example.com",
            Templates = _emailOptions.Templates
        };
        var customController = new UsersController(
            _uow.Object, _hasher.Object, _email.Object, _renderer, Options.Create(customOptions));
        customController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Scheme = "https", Host = new HostString("api.example.com") }
            }
        };

        string? capturedBody = null;
        _uow.Setup(x => x.Users.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _email.Setup(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        await customController.CreateUser(
            new CreateUserRequest("new@test.com", "Test", "User", "Password1!", null),
            CancellationToken.None);

        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("https://app.example.com/confirm-email?token=");
        capturedBody.Should().NotContain("api.example.com");
        capturedBody.Should().NotContain("localhost:5011");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Returns409()
    {
        var existing = User.Create("taken@test.com", "Other", "User", "hash");
        _uow.Setup(x => x.Users.GetByEmailAsync("taken@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _controller.CreateUser(
            new CreateUserRequest("taken@test.com", "Test", "User", "Password1!", null),
            CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
        _uow.Verify(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
