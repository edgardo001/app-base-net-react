using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Infrastructure.Notifications;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Notifications;

public class SendPasswordChangedEmailHandlerTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILogger<SendPasswordChangedEmailHandler>> _logger = new();
    private readonly SendPasswordChangedEmailHandler _handler;

    public SendPasswordChangedEmailHandlerTests()
    {
        _handler = new SendPasswordChangedEmailHandler(_email.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_PasswordChanged_CallsSendPasswordChangedEmail()
    {
        await _handler.Handle(
            new PasswordChangedNotification(Guid.NewGuid(), "u@test.com", "Test", "127.0.0.1", "ua"),
            CancellationToken.None);

        _email.Verify(x => x.SendPasswordChangedEmailAsync(
            "u@test.com", "Test", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PasswordReset_CallsSendPasswordChangedEmail()
    {
        await _handler.Handle(
            new PasswordResetNotification(Guid.NewGuid(), "u@test.com", "Test", "127.0.0.1", "ua"),
            CancellationToken.None);

        _email.Verify(x => x.SendPasswordChangedEmailAsync(
            "u@test.com", "Test", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailThrows_SwallowsExceptionAndLogs()
    {
        _email.Setup(x => x.SendPasswordChangedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var act = async () => await _handler.Handle(
            new PasswordChangedNotification(Guid.NewGuid(), "u@test.com", "Test", "127.0.0.1", "ua"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
