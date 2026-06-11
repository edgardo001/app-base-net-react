using FluentAssertions;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Domain;

public class ExternalLoginEntityTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var userId = Guid.NewGuid();
        var externalLogin = ExternalLogin.Create(userId, "google", "12345", "user@gmail.com");

        externalLogin.UserId.Should().Be(userId);
        externalLogin.Provider.Should().Be("google");
        externalLogin.ProviderId.Should().Be("12345");
        externalLogin.ProviderEmail.Should().Be("user@gmail.com");
        externalLogin.Id.Should().NotBeEmpty();
        externalLogin.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithDifferentProviders_KeepsDistinct()
    {
        var userId = Guid.NewGuid();
        var google = ExternalLogin.Create(userId, "google", "g1", "a@gmail.com");
        var microsoft = ExternalLogin.Create(userId, "microsoft", "m1", "a@outlook.com");

        google.Provider.Should().Be("google");
        google.ProviderId.Should().Be("g1");
        microsoft.Provider.Should().Be("microsoft");
        microsoft.ProviderId.Should().Be("m1");
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var userId = Guid.NewGuid();
        var el1 = ExternalLogin.Create(userId, "google", "1", "a@b.com");
        var el2 = ExternalLogin.Create(userId, "google", "2", "c@d.com");

        el1.Id.Should().NotBe(el2.Id);
    }
}
