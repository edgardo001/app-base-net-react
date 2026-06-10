using FluentAssertions;
using MediatR;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.AdminResetPassword;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.AdminResetPassword;

public class AdminResetPasswordCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IRandomPasswordGenerator> _passwords = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly AdminResetPasswordCommandHandler _handler;

    public AdminResetPasswordCommandHandlerTests()
    {
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _passwords.Setup(x => x.Generate(It.IsAny<int>())).Returns("TempPass123");
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new AdminResetPasswordCommandHandler(_uow.Object, _hasher.Object, _passwords.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ResetsPasswordAndConfirmsEmail()
    {
        var user = User.Create("test@test.com", "John", "Doe", "oldhash");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new AdminResetPasswordCommand(user.Id, "https://app.example.com/login", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.TemporaryPassword.Should().Be("TempPass123");
        user.EmailConfirmed.Should().BeTrue();
        _hasher.Verify(x => x.HashPassword("TempPass123"), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new AdminResetPasswordCommand(Guid.NewGuid(), "https://app.example.com/login", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("UserNotFound");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingUser_PublishesNotification()
    {
        var user = User.Create("test@test.com", "John", "Doe", "oldhash");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new AdminResetPasswordCommand(user.Id, "https://app.example.com/login", "10.0.0.1", "MyAgent/1.0"), CancellationToken.None);

        _mediator.Verify(x => x.Publish(
            It.Is<PasswordResetByAdminNotification>(n =>
                n.UserId == user.Id &&
                n.Email == user.Email &&
                n.IpAddress == "10.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
