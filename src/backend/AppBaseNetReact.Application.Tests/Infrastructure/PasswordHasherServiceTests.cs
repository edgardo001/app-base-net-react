using FluentAssertions;
using AppBaseNetReact.Infrastructure.Identity;

namespace AppBaseNetReact.Application.Tests.Infrastructure;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _hasher = new();

    [Fact]
    public void HashPassword_ReturnsSaltDotHash()
    {
        var hash = _hasher.HashPassword("TestPassword123!");

        hash.Should().Contain(".");
        var parts = hash.Split('.');
        parts.Should().HaveCount(2);
    }

    [Fact]
    public void HashPassword_DifferentCallsProduceDifferentHashes()
    {
        var hash1 = _hasher.HashPassword("TestPassword123!");
        var hash2 = _hasher.HashPassword("TestPassword123!");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.HashPassword("TestPassword123!");

        var result = _hasher.VerifyPassword("TestPassword123!", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("TestPassword123!");

        var result = _hasher.VerifyPassword("WrongPassword!", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_ReturnsFalse()
    {
        var result = _hasher.VerifyPassword("test", "not-a-valid-hash");

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithSinglePartHash_ReturnsFalse()
    {
        var result = _hasher.VerifyPassword("test", "onlyonepart");

        result.Should().BeFalse();
    }
}
