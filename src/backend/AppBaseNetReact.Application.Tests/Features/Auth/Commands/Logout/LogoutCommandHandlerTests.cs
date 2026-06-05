using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Commands.Logout;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new LogoutCommandHandler(_uow.Object, _jwt.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithKnownToken_RevokesAndPublishesUserLoggedOut()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, Guid.NewGuid(), "old-hash", DateTime.UtcNow.AddDays(7), "ua", "127.0.0.1");
        _jwt.Setup(x => x.HashRefreshToken("raw")).Returns("old-hash");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await _handler.Handle(
            new LogoutCommand("raw", "127.0.0.1", "ua"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        token.IsRevoked.Should().BeTrue();
        _mediator.Verify(x => x.Publish(
            It.Is<UserLoggedOutNotification>(n => n.UserId == userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_DoesNotThrowAndDoesNotPublish()
    {
        _jwt.Setup(x => x.HashRefreshToken("raw")).Returns("h");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("h", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await _handler.Handle(
            new LogoutCommand("raw", "127.0.0.1", "ua"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        _mediator.Verify(x => x.Publish(It.IsAny<UserLoggedOutNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
