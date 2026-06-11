using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Services;

public class PasswordPolicyServiceTests
{
    private static IPasswordPolicyService CreateService(Action<PasswordPolicySettings>? configure = null)
    {
        var settings = new PasswordPolicySettings();
        configure?.Invoke(settings);
        var uowMock = new Mock<IUnitOfWork>();
        var hasherMock = new Mock<IPasswordHasherService>();
        return new PasswordPolicyService(Options.Create(settings), uowMock.Object, hasherMock.Object);
    }

    [Fact]
    public void Validate_WithValidPassword_ReturnsSuccess()
    {
        var service = CreateService();
        var (valid, error) = service.Validate("Passw0rd!@");
        valid.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenTooShort_ReturnsError()
    {
        var service = CreateService(s => s.RequiredLength = 10);
        var (valid, error) = service.Validate("Ab1!x");
        valid.Should().BeFalse();
        error.Should().Contain("at least 10");
    }

    [Fact]
    public void Validate_WhenMissingUppercase_ReturnsError()
    {
        var service = CreateService(s =>
        {
            s.RequireUppercase = true;
            s.RequiredLength = 1;
        });
        var (valid, error) = service.Validate("lowercase1!");
        valid.Should().BeFalse();
        error.Should().Contain("uppercase");
    }

    [Fact]
    public void Validate_WhenMissingLowercase_ReturnsError()
    {
        var service = CreateService(s =>
        {
            s.RequireLowercase = true;
            s.RequiredLength = 1;
        });
        var (valid, error) = service.Validate("UPPERCASE1!");
        valid.Should().BeFalse();
        error.Should().Contain("lowercase");
    }

    [Fact]
    public void Validate_WhenMissingDigit_ReturnsError()
    {
        var service = CreateService(s =>
        {
            s.RequireDigit = true;
            s.RequiredLength = 1;
        });
        var (valid, error) = service.Validate("Password!");
        valid.Should().BeFalse();
        error.Should().Contain("digit");
    }

    [Fact]
    public void Validate_WhenMissingNonAlphanumeric_ReturnsError()
    {
        var service = CreateService(s =>
        {
            s.RequireNonAlphanumeric = true;
            s.RequiredLength = 1;
        });
        var (valid, error) = service.Validate("Password1");
        valid.Should().BeFalse();
        error.Should().Contain("non-alphanumeric");
    }

    [Fact]
    public void Validate_WhenAllChecksDisabled_ReturnsSuccess()
    {
        var service = CreateService(s =>
        {
            s.RequiredLength = 0;
            s.RequireNonAlphanumeric = false;
            s.RequireLowercase = false;
            s.RequireUppercase = false;
            s.RequireDigit = false;
        });
        var (valid, error) = service.Validate("");
        valid.Should().BeTrue();
    }
}
