using FluentAssertions;
using AppBaseNetReact.Infrastructure.Email;

namespace AppBaseNetReact.Application.Tests.Email;

public class EmailRendererTests
{
    private readonly EmailRenderer _renderer = new();

    [Fact]
    public void Render_WithAllVariables_ReplacesPlaceholders()
    {
        var result = _renderer.Render("welcome.html", new Dictionary<string, string>
        {
            ["UserName"] = "Juan",
            ["LoginLink"] = "https://example.com/login",
            ["Year"] = "2026"
        });

        result.Should().Contain("Juan");
        result.Should().Contain("https://example.com/login");
        result.Should().Contain("2026");
        result.Should().NotContain("{{UserName}}");
        result.Should().NotContain("{{LoginLink}}");
    }

    [Fact]
    public void Render_WithMissingVariable_Throws()
    {
        var act = () => _renderer.Render("welcome.html", new Dictionary<string, string>
        {
            ["UserName"] = "Juan"
            // Missing LoginLink and Year
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LoginLink*");
    }

    [Fact]
    public void Render_WithNonExistentTemplate_Throws()
    {
        var act = () => _renderer.Render("nonexistent.html", new Dictionary<string, string>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nonexistent.html*");
    }

    [Fact]
    public void Render_PasswordResetTemplate_HasResetLink()
    {
        var result = _renderer.Render("password-reset.html", new Dictionary<string, string>
        {
            ["UserName"] = "Maria",
            ["ResetLink"] = "https://example.com/reset?token=abc",
            ["Year"] = "2026"
        });

        result.Should().Contain("Maria");
        result.Should().Contain("https://example.com/reset?token=abc");
    }

    [Fact]
    public void Render_AccountLockedTemplate_HasLockoutMinutes()
    {
        var result = _renderer.Render("account-locked.html", new Dictionary<string, string>
        {
            ["UserName"] = "Carlos",
            ["LockoutMinutes"] = "15",
            ["ResetLink"] = "https://example.com/reset",
            ["Year"] = "2026"
        });

        result.Should().Contain("Carlos");
        result.Should().Contain("15");
    }
}
