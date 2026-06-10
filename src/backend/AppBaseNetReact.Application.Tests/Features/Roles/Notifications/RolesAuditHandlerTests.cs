using FluentAssertions;
using MediatR;
using Moq;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Notifications;
using AppBaseNetReact.Infrastructure.Notifications;

namespace AppBaseNetReact.Application.Tests.Features.Roles.Notifications;

public class RolesAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<ILogger<RoleCreatedAuditHandler>> _createdLogger = new();
    private readonly Mock<ILogger<RoleUpdatedAuditHandler>> _updatedLogger = new();
    private readonly Mock<ILogger<RoleDeletedAuditHandler>> _deletedLogger = new();
    private readonly Mock<ILogger<RolePermissionsUpdatedAuditHandler>> _permissionsLogger = new();

    [Fact]
    public async Task RoleCreatedAuditHandler_LogsCorrectAction()
    {
        var handler = new RoleCreatedAuditHandler(_audit.Object, _createdLogger.Object);
        var notification = new RoleCreatedNotification(Guid.NewGuid(), "Admin", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "RoleCreated", "Role", notification.RoleId.ToString(),
            null, null, null,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.RoleName)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleUpdatedAuditHandler_LogsCorrectAction()
    {
        var handler = new RoleUpdatedAuditHandler(_audit.Object, _updatedLogger.Object);
        var notification = new RoleUpdatedNotification(Guid.NewGuid(), "OldName", "NewName", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "RoleUpdated", "Role", notification.RoleId.ToString(),
            It.Is<string>(d => d.Contains("OldName")),
            It.Is<string>(d => d.Contains("NewName")),
            null,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains("OldName") && d.Contains("NewName")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleDeletedAuditHandler_LogsCorrectAction()
    {
        var handler = new RoleDeletedAuditHandler(_audit.Object, _deletedLogger.Object);
        var deletedBy = Guid.NewGuid();
        var notification = new RoleDeletedNotification(Guid.NewGuid(), "Admin", deletedBy, "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "RoleDeleted", "Role", notification.RoleId.ToString(),
            null, null, deletedBy,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.RoleName)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RolePermissionsUpdatedAuditHandler_LogsCorrectAction()
    {
        var handler = new RolePermissionsUpdatedAuditHandler(_audit.Object, _permissionsLogger.Object);
        var notification = new RolePermissionsUpdatedNotification(Guid.NewGuid(), "Admin", "127.0.0.1", "TestAgent");

        await handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "RolePermissionsUpdated", "Role", notification.RoleId.ToString(),
            null, null, null,
            notification.IpAddress,
            notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.RoleName)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
