using FluentAssertions;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Infrastructure;

public class AuditServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAuditLogRepository> _auditLogs = new();
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _uow.Setup(x => x.AuditLogs).Returns(_auditLogs.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new AuditService(_uow.Object);
    }

    [Fact]
    public async Task LogAsync_CreatesAuditLogAndSaves()
    {
        await _service.LogAsync(
            "UserLoggedIn", "User", Guid.NewGuid().ToString(),
            null, null, Guid.NewGuid(),
            "127.0.0.1", "Mozilla/5.0", "Login success");

        _auditLogs.Verify(x => x.AddAsync(
            It.Is<AuditLog>(a =>
                a.Action == "UserLoggedIn" &&
                a.EntityType == "User" &&
                a.IpAddress == "127.0.0.1" &&
                a.UserAgent == "Mozilla/5.0" &&
                a.Details == "Login success"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithNullOptionalFields_PassesNulls()
    {
        await _service.LogAsync(
            "TestAction", "TestEntity", null,
            null, null, null,
            "127.0.0.1", "Mozilla/5.0");

        _auditLogs.Verify(x => x.AddAsync(
            It.Is<AuditLog>(a =>
                a.EntityId == null &&
                a.UserId == null &&
                a.OldValues == null &&
                a.NewValues == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
