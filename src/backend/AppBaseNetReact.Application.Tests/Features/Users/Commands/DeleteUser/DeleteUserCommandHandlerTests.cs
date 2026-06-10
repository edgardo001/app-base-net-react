using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.DeleteUser;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new DeleteUserCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_SoftDeletes()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        var currentUserId = Guid.NewGuid();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new DeleteUserCommand(user.Id, currentUserId, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new DeleteUserCommand(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("UserNotFound");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SelfDelete_ReturnsCannotDeleteSelf()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new DeleteUserCommand(user.Id, user.Id, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("CannotDeleteSelf");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingUser_PublishesNotification()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        var currentUserId = Guid.NewGuid();
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new DeleteUserCommand(user.Id, currentUserId, "10.0.0.1", "MyAgent/1.0"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<UserDeletedNotification>(n =>
                n.UserId == user.Id &&
                n.DeletedBy == currentUserId &&
                n.IpAddress == "10.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
