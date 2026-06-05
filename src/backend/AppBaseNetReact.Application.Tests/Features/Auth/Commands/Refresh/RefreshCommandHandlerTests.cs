using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.Refresh;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.Refresh;

public class RefreshCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly RefreshCommandHandler _handler;

    public RefreshCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _uow.Setup(x => x.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken t, CancellationToken _) => t);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RefreshCommandHandler(_uow.Object, _jwt.Object, _clock.Object, _mediator.Object);
    }

    private static RefreshToken CreateActiveToken(Guid userId, DateTime expiresAt)
    {
        return RefreshToken.Create(userId, Guid.NewGuid(), "old-hash", expiresAt, "ua", "127.0.0.1");
    }

    private static User CreateActiveConfirmedUser(Guid? id = null)
    {
        var user = User.Create("u@test.com", "hash", "T", "U", id ?? Guid.NewGuid());
        user.ConfirmEmail();
        return user;
    }

    [Fact]
    public async Task Handle_WithValidToken_RotatesAndPublishesTokenRefreshed()
    {
        var user = CreateActiveConfirmedUser();
        var token = CreateActiveToken(user.Id, DateTime.UtcNow.AddDays(7));

        _jwt.Setup(x => x.HashRefreshToken("raw")).Returns("old-hash");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwt.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .Returns(("new-access", DateTime.UtcNow.AddMinutes(15)));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh");
        _jwt.Setup(x => x.HashRefreshToken("new-refresh")).Returns("new-hash");

        var outcome = await _handler.Handle(
            new RefreshCommand("raw", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(RefreshErrorCode.None);
        outcome.Response.Should().NotBeNull();
        outcome.Response!.AccessToken.Should().Be("new-access");
        outcome.Response.RefreshToken.Should().Be("new-refresh");
        token.IsRevoked.Should().BeTrue();
        _mediator.Verify(x => x.Publish(
            It.Is<TokenRefreshedNotification>(n => n.UserId == user.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsInvalidToken()
    {
        _jwt.Setup(x => x.HashRefreshToken("ghost")).Returns("ghost-hash");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("ghost-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var outcome = await _handler.Handle(
            new RefreshCommand("ghost", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(RefreshErrorCode.InvalidToken);
        outcome.Response.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithRevokedToken_RevokesAllSessionsAndPublishesReuseNotification()
    {
        var userId = Guid.NewGuid();
        var token = CreateActiveToken(userId, DateTime.UtcNow.AddDays(7));
        token.Revoke();

        _jwt.Setup(x => x.HashRefreshToken("raw")).Returns("h");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("h", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var outcome = await _handler.Handle(
            new RefreshCommand("raw", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(RefreshErrorCode.TokenCompromised);
        outcome.Response.Should().BeNull();
        _uow.Verify(x => x.RefreshTokens.RevokeAllForUserAsync(userId, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _mediator.Verify(x => x.Publish(
            It.Is<TokenReuseDetectedNotification>(n => n.UserId == userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsTokenExpired()
    {
        var token = CreateActiveToken(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));
        _jwt.Setup(x => x.HashRefreshToken("raw")).Returns("h");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("h", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var outcome = await _handler.Handle(
            new RefreshCommand("raw", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(RefreshErrorCode.TokenExpired);
        outcome.Response.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithMissingUser_ReturnsUserNotFoundOrInactive()
    {
        var userId = Guid.NewGuid();
        var token = CreateActiveToken(userId, DateTime.UtcNow.AddDays(7));
        _jwt.Setup(x => x.HashRefreshToken("raw")).Returns("h");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("h", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new RefreshCommand("raw", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(RefreshErrorCode.UserNotFoundOrInactive);
        outcome.Response.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsUserNotFoundOrInactive()
    {
        var user = CreateActiveConfirmedUser();
        user.SetActive(false);
        var token = CreateActiveToken(user.Id, DateTime.UtcNow.AddDays(7));
        _jwt.Setup(x => x.HashRefreshToken("raw")).Returns("h");
        _uow.Setup(x => x.RefreshTokens.GetByTokenHashAsync("h", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new RefreshCommand("raw", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(RefreshErrorCode.UserNotFoundOrInactive);
        outcome.Response.Should().BeNull();
    }
}
