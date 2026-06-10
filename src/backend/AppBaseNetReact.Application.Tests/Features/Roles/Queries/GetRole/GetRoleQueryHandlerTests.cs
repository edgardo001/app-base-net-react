using FluentAssertions;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Queries.GetRole;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Queries.GetRole;

public class GetRoleQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly GetRoleQueryHandler _handler;

    public GetRoleQueryHandlerTests()
    {
        _uow.Setup(x => x.Roles).Returns(_roles.Object);
        _handler = new GetRoleQueryHandler(_uow.Object);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsRole()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "Administrator", true);

        _roles.Setup(x => x.GetByIdWithPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await _handler.Handle(new GetRoleQuery(roleId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(role.Id);
        result.Name.Should().Be("Admin");
        result.Description.Should().Be("Administrator");
        result.IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenRoleNotExists_ReturnsNull()
    {
        _roles.Setup(x => x.GetByIdWithPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _handler.Handle(new GetRoleQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
