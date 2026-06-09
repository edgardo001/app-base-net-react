using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class LoginAttemptTests
{
    [Fact]
    public void Create_SuccessfulAttempt_SetsProperties()
    {
        var attempt = LoginAttempt.Create("user@test.com", "127.0.0.1", success: true);

        attempt.Email.Should().Be("user@test.com");
        attempt.IpAddress.Should().Be("127.0.0.1");
        attempt.Success.Should().BeTrue();
        attempt.FailureReason.Should().BeNull();
        attempt.Id.Should().NotBe(Guid.Empty);
        attempt.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_FailedAttempt_WithReason_SetsProperties()
    {
        var attempt = LoginAttempt.Create("user@test.com", "127.0.0.1", success: false, "Invalid credentials");

        attempt.Success.Should().BeFalse();
        attempt.FailureReason.Should().Be("Invalid credentials");
    }

    [Fact]
    public void Create_FailedAttempt_WithoutReason_SetsNull()
    {
        var attempt = LoginAttempt.Create("user@test.com", "127.0.0.1", success: false);

        attempt.FailureReason.Should().BeNull();
    }
}
