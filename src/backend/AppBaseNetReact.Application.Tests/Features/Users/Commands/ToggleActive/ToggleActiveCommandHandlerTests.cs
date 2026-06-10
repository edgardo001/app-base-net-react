using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.ToggleActive;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.ToggleActive;

public class ToggleActiveCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly ToggleActiveCommandHandler _handler;

    public ToggleActiveCommandHandlerTests()
    {
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new ToggleActiveCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_TogglesActiveState()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new ToggleActiveCommand(user.Id, false, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.IsActive.Should().BeFalse();
        user.IsActive.Should().BeFalse();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new ToggleActiveCommand(Guid.NewGuid(), true, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("UserNotFound");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithActiveTrue_PublishesActivatedNotification()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new ToggleActiveCommand(user.Id, true, "127.0.0.1", "TestAgent"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<UserActivatedNotification>(n => n.UserId == user.Id && n.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithActiveFalse_PublishesDeactivatedNotification()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new ToggleActiveCommand(user.Id, false, "127.0.0.1", "TestAgent"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<UserDeactivatedNotification>(n => n.UserId == user.Id && n.IsActive == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
