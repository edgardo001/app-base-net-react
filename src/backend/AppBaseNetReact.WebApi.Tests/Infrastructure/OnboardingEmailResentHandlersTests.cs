using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Notifications;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.WebApi.Tests.Infrastructure;

public class OnboardingEmailResentEmailHandlerTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILogger<OnboardingEmailResentEmailHandler>> _logger = new();
    private readonly EmailRenderer _renderer = new();
    private OnboardingEmailResentEmailHandler Build(EmailOptions options) =>
        new(_email.Object, _renderer, Options.Create(options), _logger.Object);

    [Fact]
    public async Task Handle_WhenEmailResendTemplateMissing_LogsWarningAndSkips()
    {
        var options = new EmailOptions
        {
            FrontendBaseUrl = "https://app",
            Templates = new Dictionary<string, EmailTemplateConfig>() // no "EmailResend"
        };

        await Build(options).Handle(
            new OnboardingEmailResentNotification(
                Guid.NewGuid(), "u@test.com", "Test", "TOKEN123", "127.0.0.1", "ua"),
            CancellationToken.None);

        _email.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "missing template config must abort the send");
        _logger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenConfigured_RendersConfirmationLinkAndSends()
    {
        var options = new EmailOptions
        {
            FrontendBaseUrl = "https://app.example.com/",
            Templates = new Dictionary<string, EmailTemplateConfig>
            {
                ["EmailResend"] = new() { Subject = "Confirm your account", TemplateFile = "email-resend.html" }
            }
        };

        await Build(options).Handle(
            new OnboardingEmailResentNotification(
                Guid.NewGuid(), "u@test.com", "Ada", "TOKENXYZ", "127.0.0.1", "ua"),
            CancellationToken.None);

        _email.Verify(x => x.SendEmailAsync(
            "u@test.com",
            "Confirm your account",
            It.Is<string>(html =>
                html.Contains("Ada") &&
                html.Contains("https://app.example.com/confirm-email?token=TOKENXYZ")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSendThrows_SwallowsAndLogsError()
    {
        var options = new EmailOptions
        {
            FrontendBaseUrl = "https://app",
            Templates = new Dictionary<string, EmailTemplateConfig>
            {
                ["EmailResend"] = new() { Subject = "x", TemplateFile = "email-resend.html" }
            }
        };
        _email.Setup(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var act = async () => await Build(options).Handle(
            new OnboardingEmailResentNotification(
                Guid.NewGuid(), "u@test.com", "Test", "TOKEN", "1.1.1.1", "ua"),
            CancellationToken.None);

        await act.Should().NotThrowAsync("notification handlers must never propagate exceptions");
        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

public class OnboardingEmailResentAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly OnboardingEmailResentAuditHandler _handler;

    public OnboardingEmailResentAuditHandlerTests()
    {
        _handler = new OnboardingEmailResentAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_LogsOnboardingEmailResentEventWithUserIdAsEntity()
    {
        var userId = Guid.NewGuid();

        await _handler.Handle(
            new OnboardingEmailResentNotification(
                userId, "u@test.com", "Test", "TOKEN", "10.0.0.1", "Mozilla/5.0"),
            CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "OnboardingEmailResent",
            "User",
            userId.ToString(),
            null,
            null,
            userId,
            "10.0.0.1",
            "Mozilla/5.0",
            null,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
