using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ChangePassword;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IPasswordPolicyService> _passwordPolicy = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(x => x.RefreshTokens.RevokeAllForUserAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _passwordPolicy.Setup(x => x.Validate(It.IsAny<string>())).Returns((true, ""));
        _passwordPolicy.Setup(x => x.CheckPasswordHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(x => x.PasswordHistories.AddAsync(It.IsAny<PasswordHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordHistory ph, CancellationToken _) => ph);
        _uow.Setup(x => x.PasswordHistories.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<PasswordHistory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new ChangePasswordCommandHandler(
            _uow.Object, _hasher.Object, _passwordPolicy.Object, _mediator.Object);
    }

    private static User CreateActiveConfirmedUser()
    {
        var user = User.Create("u@test.com", "hash", "T", "U");
        user.ConfirmEmail();
        return user;
    }

    [Fact]
    public async Task Handle_WithValidCurrentAndNewPassword_RotatesPasswordAndRevokesAllAndPublishes()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("current", user.PasswordHash)).Returns(true);
        _hasher.Setup(x => x.HashPassword("new-pwd")).Returns("new-hash");

        var outcome = await _handler.Handle(
            new ChangePasswordCommand(user.Id, "current", "new-pwd", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.None);
        _uow.Verify(x => x.RefreshTokens.RevokeAllForUserAsync(user.Id, user.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _mediator.Verify(x => x.Publish(
            It.Is<PasswordChangedNotification>(n => n.UserId == user.Id && n.Email == user.Email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMissingUser_ReturnsUserNotFound()
    {
        var userId = Guid.NewGuid();
        _uow.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new ChangePasswordCommand(userId, "current", "new-pwd", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.UserNotFound);
        _mediator.Verify(x => x.Publish(It.IsAny<PasswordChangedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ReturnsInvalidCurrentPassword()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("wrong", user.PasswordHash)).Returns(false);

        var outcome = await _handler.Handle(
            new ChangePasswordCommand(user.Id, "wrong", "new-pwd", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.InvalidCurrentPassword);
        outcome.Result.ErrorMessage.Should().Be("Current password is incorrect");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWeakNewPassword_ReturnsWeakPassword()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("current", user.PasswordHash)).Returns(true);
        _passwordPolicy.Setup(x => x.Validate("weak")).Returns((false, "Password too short"));

        var outcome = await _handler.Handle(
            new ChangePasswordCommand(user.Id, "current", "weak", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.WeakPassword);
        outcome.Result.ErrorMessage.Should().Be("Password too short");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnSuccess_PublishesNotificationWithIpAndUserAgent()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("current", user.PasswordHash)).Returns(true);
        _hasher.Setup(x => x.HashPassword("new-pwd")).Returns("new-hash");

        await _handler.Handle(
            new ChangePasswordCommand(user.Id, "current", "new-pwd", "10.0.0.1", "MyAgent/1.0"),
            CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<PasswordChangedNotification>(n =>
                n.IpAddress == "10.0.0.1" && n.UserAgent == "MyAgent/1.0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullIpAndUserAgent_PublishesNotificationWithUnknownDefaults()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("current", user.PasswordHash)).Returns(true);
        _hasher.Setup(x => x.HashPassword("new-pwd")).Returns("new-hash");

        await _handler.Handle(
            new ChangePasswordCommand(user.Id, "current", "new-pwd", null, null),
            CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<PasswordChangedNotification>(n =>
                n.IpAddress == "unknown" && n.UserAgent == "unknown"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordInHistory_ReturnsWeakPassword()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("current", user.PasswordHash)).Returns(true);
        _passwordPolicy.Setup(x => x.Validate("reused-pwd")).Returns((true, ""));
        _passwordPolicy.Setup(x => x.CheckPasswordHistoryAsync(user.Id, "reused-pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var outcome = await _handler.Handle(
            new ChangePasswordCommand(user.Id, "current", "reused-pwd", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.WeakPassword);
        outcome.Result.ErrorMessage.Should().Be("Password has been used recently. Choose a different password.");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordNotInHistory_ProceedsAndStoresNewHash()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("current", user.PasswordHash)).Returns(true);
        _hasher.Setup(x => x.HashPassword("fresh-pwd")).Returns("fresh-hash");
        _passwordPolicy.Setup(x => x.Validate("fresh-pwd")).Returns((true, ""));
        _passwordPolicy.Setup(x => x.CheckPasswordHistoryAsync(user.Id, "fresh-pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(x => x.PasswordHistories.AddAsync(It.IsAny<PasswordHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordHistory ph, CancellationToken _) => ph);
        _uow.Setup(x => x.PasswordHistories.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<PasswordHistory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _passwordPolicy.Setup(x => x.PasswordHistoryCount).Returns(5);

        var outcome = await _handler.Handle(
            new ChangePasswordCommand(user.Id, "current", "fresh-pwd", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.None);
        _uow.Verify(x => x.PasswordHistories.AddAsync(
            It.Is<PasswordHistory>(ph => ph.UserId == user.Id && ph.PasswordHash == "fresh-hash"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHistoryExceedsLimit_TrimsOldestEntry()
    {
        var user = CreateActiveConfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("current", user.PasswordHash)).Returns(true);
        _hasher.Setup(x => x.HashPassword("another-pwd")).Returns("another-hash");
        _passwordPolicy.Setup(x => x.Validate("another-pwd")).Returns((true, ""));
        _passwordPolicy.Setup(x => x.CheckPasswordHistoryAsync(user.Id, "another-pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(x => x.PasswordHistories.AddAsync(It.IsAny<PasswordHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordHistory ph, CancellationToken _) => ph);
        _uow.Setup(x => x.PasswordHistories.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<PasswordHistory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(6);
        _passwordPolicy.Setup(x => x.PasswordHistoryCount).Returns(5);

        var outcome = await _handler.Handle(
            new ChangePasswordCommand(user.Id, "current", "another-pwd", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.None);
        _uow.Verify(x => x.PasswordHistories.DeleteOldestForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
