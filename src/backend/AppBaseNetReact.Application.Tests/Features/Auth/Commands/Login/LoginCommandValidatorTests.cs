using FluentAssertions;
using AppBaseNetReact.Application.Features.Auth.Commands.Login;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithNonEmailFormatIdentifier_Passes()
    {
        var result = _validator.Validate(
            new LoginCommand("admin", "admin", "127.0.0.1", "ua", "http://localhost:5173", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidEmailFormat_Passes()
    {
        var result = _validator.Validate(
            new LoginCommand("user@example.com", "secret", "127.0.0.1", "ua", "http://localhost:5173", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyEmail_Fails()
    {
        var result = _validator.Validate(
            new LoginCommand("", "secret", "127.0.0.1", "ua", "http://localhost:5173", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithEmailOver256Chars_Fails()
    {
        var longEmail = new string('a', 251) + "@b.com";
        var result = _validator.Validate(
            new LoginCommand(longEmail, "secret", "127.0.0.1", "ua", "http://localhost:5173", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithEmptyPassword_Fails()
    {
        var result = _validator.Validate(
            new LoginCommand("admin", "", "127.0.0.1", "ua", "http://localhost:5173", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
