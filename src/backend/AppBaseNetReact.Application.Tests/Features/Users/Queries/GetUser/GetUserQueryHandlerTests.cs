using FluentAssertions;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Queries.GetUser;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Queries.GetUser;

public class GetUserQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly GetUserQueryHandler _handler;

    public GetUserQueryHandlerTests()
    {
        _handler = new GetUserQueryHandler(_uow.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsUserDetail()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");

        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new GetUserQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("test@test.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsNull()
    {
        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppBaseNetReact.Domain.Entities.User?)null);

        var result = await _handler.Handle(
            new GetUserQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithUserWithoutRoles_ReturnsEmptyRoles()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");

        _uow.Setup(x => x.Users.GetByIdWithRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new GetUserQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Roles.Should().BeEmpty();
    }
}
