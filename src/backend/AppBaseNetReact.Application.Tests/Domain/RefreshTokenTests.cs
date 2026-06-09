using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var userId = Guid.NewGuid();
        var jwtId = Guid.NewGuid();
        var expires = DateTime.UtcNow.AddDays(7);

        var token = RefreshToken.Create(userId, jwtId, "hash123", expires, "Chrome", "127.0.0.1");

        token.UserId.Should().Be(userId);
        token.JwtId.Should().Be(jwtId);
        token.TokenHash.Should().Be("hash123");
        token.ExpiresAt.Should().Be(expires);
        token.DeviceInfo.Should().Be("Chrome");
        token.IpAddress.Should().Be("127.0.0.1");
        token.Id.Should().NotBe(Guid.Empty);
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullOptionalFields_SetsDefaults()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(1), null, null);

        token.DeviceInfo.Should().BeNull();
        token.IpAddress.Should().BeNull();
    }

    [Fact]
    public void IsExpired_WhenPastExpiry_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(-1), null, null);

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenFutureExpiry_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(1), null, null);

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenNotRevoked_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(1), null, null);

        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenNotExpiredAndNotRevoked_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(1), null, null);

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(-1), null, null);

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsRevokedAtAndRevokedBy()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(1), null, null);
        var revokedBy = Guid.NewGuid();

        token.Revoke(revokedBy, "new-hash");

        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        token.RevokedBy.Should().Be(revokedBy);
        token.ReplacedByTokenHash.Should().Be("new-hash");
        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithoutReplacement_SetsNullReplacedBy()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow.AddHours(1), null, null);

        token.Revoke();

        token.RevokedAt.Should().NotBeNull();
        token.ReplacedByTokenHash.Should().BeNull();
    }
}
