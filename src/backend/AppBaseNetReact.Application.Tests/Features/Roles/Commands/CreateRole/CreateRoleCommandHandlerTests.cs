using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Commands.CreateRole;
using AppBaseNetReact.Application.Features.Roles.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _uow.Setup(x => x.Roles).Returns(_roles.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateRoleCommandHandler(_uow.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesRole()
    {
        _roles.Setup(x => x.GetByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var outcome = await _handler.Handle(
            new CreateRoleCommand("Admin", "Administrator", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.Name.Should().Be("Admin");
        _roles.Verify(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ReturnsDuplicateName()
    {
        var existing = Role.Create("Admin", "Existing");
        _roles.Setup(x => x.GetByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var outcome = await _handler.Handle(
            new CreateRoleCommand("Admin", "Duplicate", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("DuplicateName");
        _roles.Verify(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PublishesRoleCreatedNotification()
    {
        _roles.Setup(x => x.GetByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        await _handler.Handle(
            new CreateRoleCommand("Admin", "Administrator", "127.0.0.1", "TestAgent"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<RoleCreatedNotification>(n => n.RoleName == "Admin"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
