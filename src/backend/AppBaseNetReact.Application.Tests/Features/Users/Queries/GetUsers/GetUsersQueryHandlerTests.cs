using FluentAssertions;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Queries.GetUsers;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _handler = new GetUsersQueryHandler(_uow.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsPagedUsers()
    {
        var users = new List<User>
        {
            User.Create("a@test.com", "John", "Doe", "hash1"),
            User.Create("b@test.com", "Jane", "Smith", "hash2")
        };

        var pagedResult = new PagedResult<User>
        {
            Items = users,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _uow.Setup(x => x.Users.GetPagedAsync(
            1, 10, null, null, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _handler.Handle(
            new GetUsersQuery(1, 10), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithSearch_ReturnsFilteredUsers()
    {
        var users = new List<User>
        {
            User.Create("john@test.com", "John", "Doe", "hash1")
        };

        var pagedResult = new PagedResult<User>
        {
            Items = users,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _uow.Setup(x => x.Users.GetPagedAsync(
            1, 10, null, null, false, "john", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _handler.Handle(
            new GetUsersQuery(1, 10, "john"), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ReturnsEmptyList()
    {
        var pagedResult = new PagedResult<User>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _uow.Setup(x => x.Users.GetPagedAsync(
            1, 10, null, null, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _handler.Handle(
            new GetUsersQuery(1, 10), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSorting_ReturnsSortedUsers()
    {
        var users = new List<User>
        {
            User.Create("b@test.com", "hash2", "Jane", "Smith"),
            User.Create("a@test.com", "hash1", "John", "Doe")
        };

        var pagedResult = new PagedResult<User>
        {
            Items = users,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _uow.Setup(x => x.Users.GetPagedAsync(
            1, 10, null, "email", true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _handler.Handle(
            new GetUsersQuery(1, 10, null, "email", true), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].Email.Should().Be("b@test.com");
    }
}
