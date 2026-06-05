using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ConfirmEmail;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly ConfirmEmailCommandHandler _handler;

    public ConfirmEmailCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ConfirmEmailCommandHandler(_uow.Object, _clock.Object, _mediator.Object);
    }

    private static User CreateUserWithToken(DateTime expires)
    {
        var user = User.Create("u@test.com", "hash", "T", "U");
        user.SetEmailConfirmationToken("valid-token", expires);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidToken_ConfirmsAndPersistsAndPublishes()
    {
        var user = CreateUserWithToken(DateTime.UtcNow.AddHours(24));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ConfirmEmailCommand("valid-token", "http://localhost:5173/login", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(EmailErrorCode.None);
        outcome.Result.IsSuccess.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(x => x.Publish(
            It.Is<EmailConfirmedNotification>(n => n.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsInvalidConfirmationToken()
    {
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new ConfirmEmailCommand("ghost", "http://localhost:5173/login", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(EmailErrorCode.InvalidConfirmationToken);
        outcome.Result.ErrorMessage.Should().Be("Invalid confirmation token");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Publish(It.IsAny<EmailConfirmedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsConfirmationTokenExpired()
    {
        var user = CreateUserWithToken(DateTime.UtcNow.AddHours(-1));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ConfirmEmailCommand("expired", "http://localhost:5173/login", "127.0.0.1", "ua"),
            CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be(EmailErrorCode.ConfirmationTokenExpired);
        outcome.Result.ErrorMessage.Should().Be("Confirmation token has expired");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Publish(It.IsAny<EmailConfirmedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_OnSuccess_PublishesNotificationWithLoginLinkIpAndUserAgent()
    {
        var user = CreateUserWithToken(DateTime.UtcNow.AddHours(24));
        _uow.Setup(x => x.Users.GetByEmailConfirmationTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new ConfirmEmailCommand("valid-token", "https://app.example.com/login", "10.0.0.1", "MyAgent/1.0"),
            CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<EmailConfirmedNotification>(n =>
                n.UserId == user.Id &&
                n.Email == user.Email &&
                n.FirstName == user.FirstName &&
                n.LoginLink == "https://app.example.com/login" &&
                n.IpAddress == "10.0.0.1" &&
                n.UserAgent == "MyAgent/1.0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
