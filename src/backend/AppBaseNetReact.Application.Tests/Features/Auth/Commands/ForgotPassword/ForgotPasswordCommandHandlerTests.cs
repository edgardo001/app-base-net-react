using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ICaptchaService> _captcha = new();
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _captcha.Setup(x => x.IsEnabled).Returns(false);
        _handler = new ForgotPasswordCommandHandler(_uow.Object, _clock.Object, _mediator.Object, _captcha.Object);
    }

    [Fact]
    public async Task Handle_WithRegisteredEmail_SetsTokenAndPublishesNotification()
    {
        var user = User.Create("u@test.com", "hash", "T", "U");
        user.ConfirmEmail();
        _uow.Setup(x => x.Users.GetByEmailAsync("u@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ForgotPasswordCommand("u@test.com", "127.0.0.1", "ua", "http://localhost:5173", null), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.None);
        user.EmailConfirmationToken.Should().NotBeNullOrEmpty();
        user.EmailConfirmationTokenExpires.Should().BeAfter(DateTime.UtcNow);
        _mediator.Verify(x => x.Publish(
            It.Is<PasswordResetRequestedNotification>(n =>
                n.UserId == user.Id && n.ResetLink.Contains("reset-password") && n.ResetLink.Contains(user.EmailConfirmationToken)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnregisteredEmail_ReturnsSuccessWithoutSideEffects()
    {
        _uow.Setup(x => x.Users.GetByEmailAsync("ghost@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new ForgotPasswordCommand("ghost@test.com", "127.0.0.1", "ua", null, null), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.None);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Publish(
            It.IsAny<PasswordResetRequestedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithRegisteredEmail_PublishesNotificationWithIpAndUserAgent()
    {
        var user = User.Create("u@test.com", "hash", "T", "U");
        _uow.Setup(x => x.Users.GetByEmailAsync("u@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new ForgotPasswordCommand("u@test.com", "10.0.0.1", "MyAgent/1.0", "http://localhost:5173", null), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<PasswordResetRequestedNotification>(n =>
                n.IpAddress == "10.0.0.1" && n.UserAgent == "MyAgent/1.0" &&
                n.ResetLink.StartsWith("http://localhost:5173/reset-password?token=")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
