using FluentAssertions;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Queries.GetUsersByRole;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Queries.GetUsersByRole;

public class GetUsersByRoleQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GetUsersByRoleQueryHandler _handler;

    public GetUsersByRoleQueryHandlerTests()
    {
        _uow.Setup(x => x.Roles).Returns(_roles.Object);
        _uow.Setup(x => x.Users).Returns(_users.Object);
        _handler = new GetUsersByRoleQueryHandler(_uow.Object);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsUsers()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "Administrator");
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var users = new List<User>
        {
            User.Create("a@test.com", "Alice", "Smith", "hash"),
            User.Create("b@test.com", "Bob", "Jones", "hash")
        };
        _users.Setup(x => x.GetUsersByRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _handler.Handle(new GetUsersByRoleQuery(roleId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Users.Should().HaveCount(2);
        result.Users.Should().Contain(u => u.Email == "a@test.com");
        result.Users.Should().Contain(u => u.Email == "b@test.com");
    }

    [Fact]
    public async Task Handle_WhenRoleNotExists_ReturnsNull()
    {
        _roles.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _handler.Handle(new GetUsersByRoleQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNoUsers_ReturnsEmptyList()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("Admin", "Administrator");
        _roles.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _users.Setup(x => x.GetUsersByRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var result = await _handler.Handle(new GetUsersByRoleQuery(roleId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Users.Should().BeEmpty();
    }
}
