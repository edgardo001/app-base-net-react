using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash", Guid.NewGuid());
        user.Email.Should().Be("test@test.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.IsActive.Should().BeTrue();
        user.AccessFailedCount.Should().Be(0);
        user.PasswordHash.Should().Be("hash");
    }

    [Fact]
    public void UpdateProfile_ChangesNames()
    {
        var user = User.Create("a@b.com", "Old", "Name", "hash", null);
        user.UpdateProfile("New", "Name2");
        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("Name2");
    }

    [Fact]
    public void MarkLogin_ResetsFailedCount()
    {
        var user = User.Create("a@b.com", "F", "L", "hash", null);
        user.IncrementFailedAccess();
        user.IncrementFailedAccess();
        user.MarkLogin();
        user.AccessFailedCount.Should().Be(0);
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IncrementFailedAccess_IncreasesCount()
    {
        var user = User.Create("a@b.com", "F", "L", "hash", null);
        user.IncrementFailedAccess();
        user.AccessFailedCount.Should().Be(1);
        user.IncrementFailedAccess();
        user.AccessFailedCount.Should().Be(2);
    }

    [Fact]
    public void SetPasswordHash_UpdatesHashAndStamp()
    {
        var user = User.Create("a@b.com", "F", "L", "oldhash", null);
        var oldStamp = user.SecurityStamp;
        user.SetPasswordHash("newhash");
        user.PasswordHash.Should().Be("newhash");
        user.SecurityStamp.Should().NotBe(oldStamp);
    }

    [Fact]
    public void SoftDelete_SetsDeletedAt()
    {
        var user = User.Create("a@b.com", "F", "L", "hash", null);
        user.SoftDelete(Guid.NewGuid());
        user.DeletedAt.Should().NotBeNull();
        user.UpdatedBy.Should().NotBeNull();
    }
}
