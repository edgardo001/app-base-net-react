using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Infrastructure.Notifications;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Notifications;

public class EmailConfirmedEmailHandlerTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILogger<EmailConfirmedEmailHandler>> _logger = new();
    private readonly EmailConfirmedEmailHandler _handler;

    public EmailConfirmedEmailHandlerTests()
    {
        _handler = new EmailConfirmedEmailHandler(_email.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_CallsSendWelcomeEmailWithEmailNameAndLoginLink()
    {
        var userId = Guid.NewGuid();
        await _handler.Handle(
            new EmailConfirmedNotification(userId, "u@test.com", "Test", "127.0.0.1", "ua", "https://app/login"),
            CancellationToken.None);

        _email.Verify(x => x.SendWelcomeEmailAsync(
            "u@test.com", "Test", "https://app/login", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailThrows_SwallowsExceptionAndLogs()
    {
        _email.Setup(x => x.SendWelcomeEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var act = async () => await _handler.Handle(
            new EmailConfirmedNotification(Guid.NewGuid(), "u@test.com", "Test", "127.0.0.1", "ua", "https://app/login"),
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
