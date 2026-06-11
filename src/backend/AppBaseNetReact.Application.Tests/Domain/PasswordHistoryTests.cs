using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class PasswordHistoryTests
{
    [Fact]
    public void Create_WithUserIdAndHash_SetsProperties()
    {
        var userId = Guid.NewGuid();
        const string hash = "hashed-password-value";

        var entry = PasswordHistory.Create(userId, hash);

        entry.UserId.Should().Be(userId);
        entry.PasswordHash.Should().Be(hash);
        entry.Id.Should().NotBeEmpty();
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_TwoInstances_HaveDifferentIds()
    {
        var userId = Guid.NewGuid();

        var entry1 = PasswordHistory.Create(userId, "hash1");
        var entry2 = PasswordHistory.Create(userId, "hash2");

        entry1.Id.Should().NotBe(entry2.Id);
    }
}
