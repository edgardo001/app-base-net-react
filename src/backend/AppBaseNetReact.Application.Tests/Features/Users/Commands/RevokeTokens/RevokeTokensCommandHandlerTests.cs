using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.RevokeTokens;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.RevokeTokens;

public class RevokeTokensCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly RevokeTokensCommandHandler _handler;

    public RevokeTokensCommandHandlerTests()
    {
        _uow.Setup(x => x.RefreshTokens).Returns(_refreshTokens.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new RevokeTokensCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_RevokesAllTokens()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new RevokeTokensCommand(user.Id, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        _refreshTokens.Verify(x => x.RevokeAllForUserAsync(user.Id, null, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new RevokeTokensCommand(Guid.NewGuid(), "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("UserNotFound");
        _refreshTokens.Verify(x => x.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingUser_PublishesNotification()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new RevokeTokensCommand(user.Id, "10.0.0.1", "MyAgent/1.0"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<TokensRevokedNotification>(n =>
                n.UserId == user.Id &&
                n.IpAddress == "10.0.0.1" &&
                n.UserAgent == "MyAgent/1.0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
