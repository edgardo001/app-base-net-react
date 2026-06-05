using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ResetPassword;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IPasswordPolicyService> _passwordPolicy = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _passwordPolicy.Setup(x => x.Validate(It.IsAny<string>())).Returns((true, ""));
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("new-hash");

        _handler = new ResetPasswordCommandHandler(
            _uow.Object, _hasher.Object, _passwordPolicy.Object, _clock.Object, _mediator.Object);
    }

    private static User CreateUserWithToken(DateTime expires)
    {
        var user = User.Create("u@test.com", "hash", "T", "U");
        user.SetEmailConfirmationToken("valid-token", expires);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidToken_ResetsPasswordAndForcesChangeAndPublishes()
    {
        var user = CreateUserWithToken(DateTime.UtcNow.AddHours(24));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ResetPasswordCommand("valid-token", "new-pwd", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.None);
        user.EmailConfirmed.Should().BeTrue();
        user.LastPasswordChangeAt.Should().BeNull();
        _mediator.Verify(x => x.Publish(
            It.Is<PasswordResetNotification>(n => n.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsInvalidResetToken()
    {
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new ResetPasswordCommand("ghost", "new-pwd", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.InvalidResetToken);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsResetTokenExpired()
    {
        var user = CreateUserWithToken(DateTime.UtcNow.AddHours(-1));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ResetPasswordCommand("expired", "new-pwd", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.ResetTokenExpired);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWeakNewPassword_ReturnsWeakPassword()
    {
        var user = CreateUserWithToken(DateTime.UtcNow.AddHours(24));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordPolicy.Setup(x => x.Validate("weak")).Returns((false, "Password too short"));

        var outcome = await _handler.Handle(
            new ResetPasswordCommand("valid-token", "weak", "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.WeakPassword);
        outcome.Result.ErrorMessage.Should().Be("Password too short");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnSuccess_PublishesNotificationWithIpAndUserAgent()
    {
        var user = CreateUserWithToken(DateTime.UtcNow.AddHours(24));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new ResetPasswordCommand("valid-token", "new-pwd", "10.0.0.1", "MyAgent/1.0"),
            CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<PasswordResetNotification>(n =>
                n.IpAddress == "10.0.0.1" && n.UserAgent == "MyAgent/1.0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
