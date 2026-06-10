using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Commands.UpdatePermissions;
using AppBaseNetReact.Application.Features.Roles.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Commands.UpdatePermissions;

public class UpdatePermissionsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly UpdatePermissionsCommandHandler _handler;

    public UpdatePermissionsCommandHandlerTests()
    {
        _uow.Setup(x => x.Roles).Returns(_roles.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdatePermissionsCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_UpdatesPermissions()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "desc");
        _roles.Setup(x => x.GetByIdWithPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var permId = Guid.NewGuid();
        var outcome = await _handler.Handle(
            new UpdatePermissionsCommand(roleId,
                new List<PermissionAssignmentDto> { new(permId, true) },
                "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoleNotExists_ReturnsNotFound()
    {
        _roles.Setup(x => x.GetByIdWithPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var outcome = await _handler.Handle(
            new UpdatePermissionsCommand(Guid.NewGuid(),
                new List<PermissionAssignmentDto> { new(Guid.NewGuid(), true) },
                "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_PublishesRolePermissionsUpdatedNotification()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "desc");
        _roles.Setup(x => x.GetByIdWithPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        await _handler.Handle(
            new UpdatePermissionsCommand(roleId,
                new List<PermissionAssignmentDto> { new(Guid.NewGuid(), true) },
                "127.0.0.1", "TestAgent"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<RolePermissionsUpdatedNotification>(n => n.RoleName == "Admin"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
