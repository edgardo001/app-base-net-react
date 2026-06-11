using FluentAssertions;
using AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandValidatorTests
{
    private readonly GoogleLoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(
            new GoogleLoginCommand("valid-code", "valid-state", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCode_Fails()
    {
        var result = _validator.Validate(
            new GoogleLoginCommand("", "valid-state", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_WithEmptyState_Fails()
    {
        var result = _validator.Validate(
            new GoogleLoginCommand("valid-code", "", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "State");
    }

    [Fact]
    public void Validate_WithBothEmpty_FailsWithTwoErrors()
    {
        var result = _validator.Validate(
            new GoogleLoginCommand("", "", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
