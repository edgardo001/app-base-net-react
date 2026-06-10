using FluentAssertions;
using MediatR;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Notifications;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Features.Users.Notifications;

public class UsersEmailHandlerTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly EmailRenderer _renderer = new();
    private readonly EmailOptions _emailOptions = new()
    {
        FrontendBaseUrl = "http://localhost:5173",
        Templates = new Dictionary<string, EmailTemplateConfig>
        {
            ["EmailConfirmation"] = new() { Subject = "Confirma tu correo", TemplateFile = "email-confirmation.html" },
            ["TemporaryPassword"] = new() { Subject = "Contraseña temporal", TemplateFile = "temporary-password.html" }
        }
    };
    private readonly Mock<ILogger<UserCreatedEmailHandler>> _createdLogger = new();
    private readonly Mock<ILogger<PasswordResetByAdminEmailHandler>> _resetLogger = new();

    [Fact]
    public async Task UserCreatedEmailHandler_SendsConfirmationEmail()
    {
        var handler = new UserCreatedEmailHandler(
            _email.Object, _renderer, Options.Create(_emailOptions), _createdLogger.Object);

        var notification = new UserCreatedNotification(
            Guid.NewGuid(), "test@test.com", "John", "token123", "TmpPass123",
            "http://localhost:5173", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _email.Verify(x => x.SendEmailAsync(
            "test@test.com",
            "Confirma tu correo",
            It.Is<string>(b => b.Contains("TmpPass123") && b.Contains("http://localhost:5173/confirm-email?token=token123")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserCreatedEmailHandler_UsesCustomFrontendBaseUrl()
    {
        var customOptions = new EmailOptions
        {
            FrontendBaseUrl = "https://app.example.com",
            Templates = _emailOptions.Templates
        };
        var handler = new UserCreatedEmailHandler(
            _email.Object, _renderer, Options.Create(customOptions), _createdLogger.Object);

        var notification = new UserCreatedNotification(
            Guid.NewGuid(), "test@test.com", "John", "token456", "MyPass456",
            "https://app.example.com", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _email.Verify(x => x.SendEmailAsync(
            "test@test.com",
            "Confirma tu correo",
            It.Is<string>(b => b.Contains("https://app.example.com/confirm-email?token=token456")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserCreatedEmailHandler_DoesNotThrowOnEmailFailure()
    {
        _email.Setup(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP error"));

        var handler = new UserCreatedEmailHandler(
            _email.Object, _renderer, Options.Create(_emailOptions), _createdLogger.Object);

        var notification = new UserCreatedNotification(
            Guid.NewGuid(), "test@test.com", "John", "token", "Pass",
            "http://localhost:5173", "127.0.0.1", "TestAgent");

        var act = () => handler.Handle(notification, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PasswordResetByAdminEmailHandler_SendsTemporaryPasswordEmail()
    {
        var handler = new PasswordResetByAdminEmailHandler(
            _email.Object, _renderer, Options.Create(_emailOptions), _resetLogger.Object);

        var notification = new PasswordResetByAdminNotification(
            Guid.NewGuid(), "test@test.com", "John", "TempPass123",
            "https://app.example.com/login", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _email.Verify(x => x.SendEmailAsync(
            "test@test.com",
            "Contraseña temporal",
            It.Is<string>(b => b.Contains("TempPass123") && b.Contains("https://app.example.com/login")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PasswordResetByAdminEmailHandler_DoesNotThrowOnEmailFailure()
    {
        _email.Setup(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP error"));

        var handler = new PasswordResetByAdminEmailHandler(
            _email.Object, _renderer, Options.Create(_emailOptions), _resetLogger.Object);

        var notification = new PasswordResetByAdminNotification(
            Guid.NewGuid(), "test@test.com", "John", "Pass",
            "https://app.example.com/login", "127.0.0.1", "TestAgent");

        var act = () => handler.Handle(notification, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
