using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.UpdateUser;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateUserCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_UpdatesProfile()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new UpdateUserCommand(user.Id, "Jane", "Smith", null, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "Jane", "Smith", null, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("UserNotFound");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRoleIds_ReassignsRoles()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        var roleIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new UpdateUserCommand(user.Id, "John", "Doe", roleIds, "127.0.0.1", "TestAgent"), CancellationToken.None);

        user.UserRoles.Should().HaveCount(2);
        _mediator.Verify(x => x.Publish(
            It.Is<UserUpdatedNotification>(n => n.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
