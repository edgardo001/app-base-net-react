using FluentAssertions;
using MediatR;
using Moq;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Infrastructure.Notifications;

namespace AppBaseNetReact.Application.Tests.Features.Users.Notifications;

public class UsersAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<ILogger<UserCreatedAuditHandler>> _createdLogger = new();
    private readonly Mock<ILogger<UserUpdatedAuditHandler>> _updatedLogger = new();
    private readonly Mock<ILogger<UserDeletedAuditHandler>> _deletedLogger = new();
    private readonly Mock<ILogger<UserActivatedAuditHandler>> _activatedLogger = new();
    private readonly Mock<ILogger<UserDeactivatedAuditHandler>> _deactivatedLogger = new();
    private readonly Mock<ILogger<PasswordResetByAdminAuditHandler>> _resetLogger = new();
    private readonly Mock<ILogger<TokensRevokedAuditHandler>> _revokedLogger = new();
    private readonly Mock<ILogger<AvatarUpdatedAuditHandler>> _avatarLogger = new();

    [Fact]
    public async Task UserCreatedAuditHandler_LogsCorrectAction()
    {
        var handler = new UserCreatedAuditHandler(_audit.Object, _createdLogger.Object);
        var notification = new UserCreatedNotification(Guid.NewGuid(), "test@test.com", "John", "token123", "TmpPass123", "http://localhost:5173", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserCreated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.Email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserUpdatedAuditHandler_LogsCorrectAction()
    {
        var handler = new UserUpdatedAuditHandler(_audit.Object, _updatedLogger.Object);
        var notification = new UserUpdatedNotification(Guid.NewGuid(), "test@test.com", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserUpdated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.Email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserDeletedAuditHandler_LogsCorrectAction()
    {
        var handler = new UserDeletedAuditHandler(_audit.Object, _deletedLogger.Object);
        var deletedBy = Guid.NewGuid();
        var notification = new UserDeletedNotification(Guid.NewGuid(), "test@test.com", deletedBy, "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserDeleted", "User", notification.UserId.ToString(),
            null, null, deletedBy,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.Email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserActivatedAuditHandler_LogsActivatedAction()
    {
        var handler = new UserActivatedAuditHandler(_audit.Object, _activatedLogger.Object);
        var notification = new UserActivatedNotification(Guid.NewGuid(), "test@test.com", true, "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserActivated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains("activated")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserDeactivatedAuditHandler_LogsDeactivatedAction()
    {
        var handler = new UserDeactivatedAuditHandler(_audit.Object, _deactivatedLogger.Object);
        var notification = new UserDeactivatedNotification(Guid.NewGuid(), "test@test.com", false, "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserDeactivated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains("deactivated")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PasswordResetByAdminAuditHandler_LogsCorrectAction()
    {
        var handler = new PasswordResetByAdminAuditHandler(_audit.Object, _resetLogger.Object);
        var notification = new PasswordResetByAdminNotification(Guid.NewGuid(), "test@test.com", "John", "TempPass123", "https://app.example.com/login", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "PasswordResetByAdmin", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.Email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TokensRevokedAuditHandler_LogsCorrectAction()
    {
        var handler = new TokensRevokedAuditHandler(_audit.Object, _revokedLogger.Object);
        var notification = new TokensRevokedNotification(Guid.NewGuid(), "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "TokensRevoked", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AvatarUpdatedAuditHandler_LogsCorrectAction()
    {
        var handler = new AvatarUpdatedAuditHandler(_audit.Object, _avatarLogger.Object);
        var notification = new AvatarUpdatedNotification(Guid.NewGuid(), "test@test.com", "avatar.jpg", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "AvatarUpdated", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.FileName)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
