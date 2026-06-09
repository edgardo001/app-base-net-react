using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class AuditLogTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var userId = Guid.NewGuid();

        var log = AuditLog.Create(
            "UserLoggedIn", "User", userId.ToString(),
            null, null,
            "127.0.0.1", "Mozilla/5.0",
            userId, "Login success");

        log.Action.Should().Be("UserLoggedIn");
        log.EntityType.Should().Be("User");
        log.EntityId.Should().Be(userId.ToString());
        log.OldValues.Should().BeNull();
        log.NewValues.Should().BeNull();
        log.IpAddress.Should().Be("127.0.0.1");
        log.UserAgent.Should().Be("Mozilla/5.0");
        log.UserId.Should().Be(userId);
        log.Details.Should().Be("Login success");
        log.Id.Should().NotBe(Guid.Empty);
        log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithOldAndNewValues_SetsSerializedData()
    {
        var log = AuditLog.Create(
            "RoleUpdated", "Role", Guid.NewGuid().ToString(),
            "{\"Name\":\"Old\"}", "{\"Name\":\"New\"}",
            "127.0.0.1", "Mozilla/5.0");

        log.OldValues.Should().Be("{\"Name\":\"Old\"}");
        log.NewValues.Should().Be("{\"Name\":\"New\"}");
    }

    [Fact]
    public void Create_WithoutOptionalFields_SetsNulls()
    {
        var log = AuditLog.Create(
            "TestAction", "TestEntity", null,
            null, null,
            "127.0.0.1", "Mozilla/5.0");

        log.EntityId.Should().BeNull();
        log.UserId.Should().BeNull();
        log.Details.Should().BeNull();
    }
}
