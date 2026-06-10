using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.CreateUser;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IRandomPasswordGenerator> _passwords = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _uow.Setup(x => x.Users).Returns(_users.Object);
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _passwords.Setup(x => x.Generate(It.IsAny<int>())).Returns("TmpPass123Abc");
        _users.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateUserCommandHandler(_uow.Object, _hasher.Object, _passwords.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesUser()
    {
        _users.Setup(x => x.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new CreateUserCommand("new@test.com", "John", "Doe", null, "http://localhost:5173", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.UserId.Should().NotBeNull();
        outcome.Result.Email.Should().Be("new@test.com");
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsDuplicateEmail()
    {
        var existing = User.Create("existing@test.com", "John", "Doe", "hash");
        _users.Setup(x => x.GetByEmailAsync("existing@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var outcome = await _handler.Handle(
            new CreateUserCommand("existing@test.com", "John", "Doe", null, "http://localhost:5173", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("DuplicateEmail");
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRoleIds_AssignsRoles()
    {
        _users.Setup(x => x.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var roleIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var outcome = await _handler.Handle(
            new CreateUserCommand("new@test.com", "John", "Doe", roleIds, "http://localhost:5173", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        _mediator.Verify(x => x.Publish(
            It.Is<UserCreatedNotification>(n => n.Email == "new@test.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GeneratesConfirmationToken()
    {
        _users.Setup(x => x.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        _users.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        await _handler.Handle(
            new CreateUserCommand("new@test.com", "John", "Doe", null, "http://localhost:5173", "127.0.0.1", "TestAgent"), CancellationToken.None);

        capturedUser.Should().NotBeNull();
        capturedUser!.EmailConfirmationToken.Should().NotBeNullOrEmpty();
        capturedUser.EmailConfirmationTokenExpires.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
    }
}
