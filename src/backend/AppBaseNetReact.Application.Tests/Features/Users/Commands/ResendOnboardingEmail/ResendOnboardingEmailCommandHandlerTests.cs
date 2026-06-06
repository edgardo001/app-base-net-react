using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.ResendOnboardingEmail;

public class ResendOnboardingEmailCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly ResendOnboardingEmailCommandHandler _handler;

    public ResendOnboardingEmailCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new ResendOnboardingEmailCommandHandler(_uow.Object, _clock.Object, _mediator.Object);
    }

    private static User CreateUnconfirmedUser()
    {
        var user = User.Create("u@test.com", "hash", "T", "U");
        user.SetEmailConfirmationToken("old-token", DateTime.UtcNow.AddHours(1));
        return user;
    }

    [Fact]
    public async Task Handle_WithUnconfirmedUser_RegeneratesTokenAndPublishes()
    {
        var user = CreateUnconfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ResendOnboardingEmailCommand(user.Id, "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        user.EmailConfirmationToken.Should().NotBeNullOrEmpty();
        user.EmailConfirmationToken.Should().NotBe("old-token");
        user.EmailConfirmationTokenExpires.Should().BeCloseTo(_clock.Object.UtcNow.AddHours(24), TimeSpan.FromSeconds(2));
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(x => x.Publish(
            It.Is<OnboardingEmailResentNotification>(n => n.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new ResendOnboardingEmailCommand(Guid.NewGuid(), "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(ResendOnboardingErrorCode.UserNotFound);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Publish(It.IsAny<OnboardingEmailResentNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyConfirmedUser_ReturnsAlreadyConfirmed()
    {
        var user = CreateUnconfirmedUser();
        user.ConfirmEmail();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ResendOnboardingEmailCommand(user.Id, "127.0.0.1", "ua"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(ResendOnboardingErrorCode.AlreadyConfirmed);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Publish(It.IsAny<OnboardingEmailResentNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnSuccess_PublishesNotificationWithIpAndUserAgent()
    {
        var user = CreateUnconfirmedUser();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await _handler.Handle(
            new ResendOnboardingEmailCommand(user.Id, "10.0.0.1", "MyAgent/1.0"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<OnboardingEmailResentNotification>(n =>
                n.IpAddress == "10.0.0.1" &&
                n.UserAgent == "MyAgent/1.0" &&
                !string.IsNullOrEmpty(n.NewConfirmationToken)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
