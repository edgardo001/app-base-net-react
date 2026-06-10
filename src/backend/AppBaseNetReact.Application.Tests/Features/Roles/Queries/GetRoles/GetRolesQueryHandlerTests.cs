using FluentAssertions;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Queries.GetRoles;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Queries.GetRoles;

public class GetRolesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly GetRolesQueryHandler _handler;

    public GetRolesQueryHandlerTests()
    {
        _uow.Setup(x => x.Roles).Returns(_roles.Object);
        _handler = new GetRolesQueryHandler(_uow.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllRoles()
    {
        var roles = new List<Role>
        {
            Role.Create("Admin", "Administrator", true),
            Role.Create("User", "Regular user")
        };
        _roles.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var result = await _handler.Handle(new GetRolesQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(r => r.Name == "Admin");
        result.Items.Should().Contain(r => r.Name == "User");
    }

    [Fact]
    public async Task Handle_WhenNoRoles_ReturnsEmptyList()
    {
        _roles.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        var result = await _handler.Handle(new GetRolesQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsRolePropertiesCorrectly()
    {
        var roles = new List<Role>
        {
            Role.Create("Admin", "Administrator", true)
        };
        _roles.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var result = await _handler.Handle(new GetRolesQuery(), CancellationToken.None);

        var role = result.Items.First();
        role.Name.Should().Be("Admin");
        role.Description.Should().Be("Administrator");
        role.IsSystem.Should().BeTrue();
        role.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
