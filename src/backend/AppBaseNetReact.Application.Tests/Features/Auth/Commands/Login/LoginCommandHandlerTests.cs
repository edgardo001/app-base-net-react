using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.Login;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IPasswordPolicyService> _passwordPolicy = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _passwordPolicy.Setup(x => x.MaxFailedAccessAttempts).Returns(5);
        _passwordPolicy.Setup(x => x.DefaultLockoutMinutes).Returns(15);
        _clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _uow.Setup(x => x.LoginAttempts.AddAsync(It.IsAny<LoginAttempt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginAttempt a, CancellationToken _) => a);
        _uow.Setup(x => x.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken t, CancellationToken _) => t);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new LoginCommandHandler(
            _uow.Object, _jwt.Object, _hasher.Object,
            _passwordPolicy.Object, _clock.Object, _mediator.Object);
    }

    private static User CreateActiveConfirmedUser(string email = "active@test.com")
    {
        var user = User.Create(email, "hash", "Test", "User", Guid.NewGuid());
        user.ConfirmEmail();
        return user;
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("plain", user.PasswordHash)).Returns(true);
        _jwt.Setup(x => x.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>()))
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _jwt.Setup(x => x.HashRefreshToken("refresh-token")).Returns("refresh-hash");

        var outcome = await _handler.Handle(
            new LoginCommand(user.Email, "plain", "127.0.0.1", "ua", "http://localhost:5173"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.None);
        outcome.Response.Should().NotBeNull();
        outcome.Response!.AccessToken.Should().Be("access-token");
        outcome.Response.RefreshToken.Should().Be("refresh-token");
        user.LastLoginAt.Should().NotBeNull();
        user.AccessFailedCount.Should().Be(0);
        _mediator.Verify(x => x.Publish(
            It.Is<UserLoggedInNotification>(n => n.UserId == user.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ReturnsInvalidCredentialsAndIncrementsFailedCount()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("wrong", user.PasswordHash)).Returns(false);

        var outcome = await _handler.Handle(
            new LoginCommand(user.Email, "wrong", "127.0.0.1", "ua", null),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.InvalidCredentials);
        outcome.Response.Should().BeNull();
        user.AccessFailedCount.Should().Be(1);
        _mediator.Verify(x => x.Publish(
            It.IsAny<UserLoginFailedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsInvalidCredentialsWithoutIncrement()
    {
        _uow.Setup(x => x.Users.GetByEmailAsync("ghost@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new LoginCommand("ghost@test.com", "any", "127.0.0.1", "ua", null),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.InvalidCredentials);
        outcome.Response.Should().BeNull();
        _uow.Verify(x => x.LoginAttempts.AddAsync(
            It.Is<LoginAttempt>(a => a.Email == "ghost@test.com" && !a.Success),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(x => x.Publish(
            It.IsAny<UserLoginFailedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDeactivatedAccount_ReturnsAccountDeactivated()
    {
        var user = CreateActiveConfirmedUser();
        user.SetActive(false);
        _uow.Setup(x => x.Users.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("plain", user.PasswordHash)).Returns(true);

        var outcome = await _handler.Handle(
            new LoginCommand(user.Email, "plain", "127.0.0.1", "ua", null),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.AccountDeactivated);
        outcome.Result.ErrorMessage.Should().Be("Account is deactivated");
    }

    [Fact]
    public async Task Handle_WithLockedAccount_ReturnsAccountLockedWithRemainingMinutes()
    {
        var user = CreateActiveConfirmedUser();
        user.LockUntil(DateTime.UtcNow.AddMinutes(10));
        _uow.Setup(x => x.Users.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("plain", user.PasswordHash)).Returns(true);

        var outcome = await _handler.Handle(
            new LoginCommand(user.Email, "plain", "127.0.0.1", "ua", null),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.AccountLocked);
        outcome.Result.RemainingLockoutMinutes.Should().NotBeNull();
        outcome.Result.RemainingLockoutMinutes!.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithUnconfirmedEmail_ReturnsEmailNotConfirmed()
    {
        var user = User.Create("u@test.com", "hash", "T", "U", Guid.NewGuid());
        _uow.Setup(x => x.Users.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("plain", user.PasswordHash)).Returns(true);

        var outcome = await _handler.Handle(
            new LoginCommand(user.Email, "plain", "127.0.0.1", "ua", null),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.EmailNotConfirmed);
        outcome.Result.ErrorMessage.Should().Contain("Email not confirmed");
    }

    [Fact]
    public async Task Handle_AfterMaxFailedAttempts_LocksAccountAndPublishesAccountLocked()
    {
        var user = CreateActiveConfirmedUser();
        for (int i = 0; i < 5; i++) user.IncrementFailedAccess();
        _uow.Setup(x => x.Users.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("wrong", user.PasswordHash)).Returns(false);

        var outcome = await _handler.Handle(
            new LoginCommand(user.Email, "wrong", "127.0.0.1", "ua", "http://localhost:5173"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.InvalidCredentials);
        user.IsLocked().Should().BeTrue();
        _mediator.Verify(x => x.Publish(
            It.Is<AccountLockedNotification>(n => n.UserId == user.Id && n.LockoutMinutes == 15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsPasswordExpiredFlag_WhenPasswordExpired()
    {
        var user = CreateActiveConfirmedUser();
        user.ForcePasswordChange();
        _uow.Setup(x => x.Users.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("plain", user.PasswordHash)).Returns(true);
        _jwt.Setup(x => x.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>()))
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _jwt.Setup(x => x.HashRefreshToken("refresh-token")).Returns("refresh-hash");

        var outcome = await _handler.Handle(
            new LoginCommand(user.Email, "plain", "127.0.0.1", "ua", null),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(LoginErrorCode.None);
        outcome.Response!.PasswordExpired.Should().BeTrue();
    }
}
