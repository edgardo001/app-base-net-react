using FluentAssertions;
using AppBaseNetReact.Application.Features.Auth.Commands.GitHubLogin;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.GitHubLogin;

public class GitHubLoginCommandValidatorTests
{
    private readonly GitHubLoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(
            new GitHubLoginCommand("valid-code", "valid-state", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCode_Fails()
    {
        var result = _validator.Validate(
            new GitHubLoginCommand("", "valid-state", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_WithEmptyState_Fails()
    {
        var result = _validator.Validate(
            new GitHubLoginCommand("valid-code", "", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "State");
    }

    [Fact]
    public void Validate_WithBothEmpty_FailsWithTwoErrors()
    {
        var result = _validator.Validate(
            new GitHubLoginCommand("", "", null, null, "http://localhost:5173"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
