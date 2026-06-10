using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Commands.UpdateRole;
using AppBaseNetReact.Application.Features.Roles.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly UpdateRoleCommandHandler _handler;

    public UpdateRoleCommandHandlerTests()
    {
        _uow.Setup(x => x.Roles).Returns(_roles.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateRoleCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_UpdatesRole()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("OldName", "Old Desc");
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var outcome = await _handler.Handle(
            new UpdateRoleCommand(roleId, "NewName", "New Desc", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoleNotExists_ReturnsNotFound()
    {
        _roles.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var outcome = await _handler.Handle(
            new UpdateRoleCommand(Guid.NewGuid(), "Name", "Desc", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_WhenSystemRole_ReturnsCannotModifySystemRole()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("SuperAdmin", "System", isSystem: true);
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var outcome = await _handler.Handle(
            new UpdateRoleCommand(roleId, "NewName", "New Desc", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("CannotModifySystemRole");
    }

    [Fact]
    public async Task Handle_PublishesRoleUpdatedNotification()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("OldName", "Old Desc");
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        await _handler.Handle(
            new UpdateRoleCommand(roleId, "NewName", "New Desc", "127.0.0.1", "TestAgent"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<RoleUpdatedNotification>(n => n.OldName == "OldName" && n.NewName == "NewName"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
