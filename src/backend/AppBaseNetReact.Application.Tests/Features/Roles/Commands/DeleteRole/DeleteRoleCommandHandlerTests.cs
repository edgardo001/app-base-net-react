using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Commands.DeleteRole;
using AppBaseNetReact.Application.Features.Roles.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleCommandHandlerTests()
    {
        _uow.Setup(x => x.Roles).Returns(_roles.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new DeleteRoleCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_DeletesRole()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("ToDelete", "desc");
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var outcome = await _handler.Handle(
            new DeleteRoleCommand(roleId, null, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        _roles.Verify(x => x.DeleteAsync(role, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoleNotExists_ReturnsNotFound()
    {
        _roles.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var outcome = await _handler.Handle(
            new DeleteRoleCommand(Guid.NewGuid(), null, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_WhenSystemRole_ReturnsCannotDeleteSystemRole()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("SuperAdmin", "System", isSystem: true);
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var outcome = await _handler.Handle(
            new DeleteRoleCommand(roleId, null, "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("CannotDeleteSystemRole");
    }

    [Fact]
    public async Task Handle_PublishesRoleDeletedNotification()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("ToDelete", "desc");
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        await _handler.Handle(
            new DeleteRoleCommand(roleId, null, "127.0.0.1", "TestAgent"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<RoleDeletedNotification>(n => n.RoleName == "ToDelete"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
