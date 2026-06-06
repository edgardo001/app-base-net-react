using FluentAssertions;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.WebApi.Tests.Infrastructure;

public class RandomPasswordGeneratorTests
{
    private readonly RandomPasswordGenerator _gen = new();

    [Fact]
    public void Generate_DefaultLength_Is12()
    {
        var pwd = _gen.Generate();
        pwd.Should().HaveLength(12);
    }

    [Fact]
    public void Generate_HonorsCustomLength()
    {
        var pwd = _gen.Generate(20);
        pwd.Should().HaveLength(20);
    }

    [Fact]
    public void Generate_ContainsAtLeastOneOfEachCharacterClass()
    {
        // Run several times to be robust against rare distribution tails.
        foreach (var _ in Enumerable.Range(0, 50))
        {
            var pwd = _gen.Generate();
            pwd.Should().Match(p => p!.Any(char.IsUpper), "at least one uppercase letter");
            pwd.Should().Match(p => p!.Any(char.IsLower), "at least one lowercase letter");
            pwd.Should().Match(p => p!.Any(char.IsDigit), "at least one digit");
        }
    }

    [Fact]
    public void Generate_OnlyUsesSafeCharset()
    {
        var allowed = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
        foreach (var _ in Enumerable.Range(0, 20))
        {
            var pwd = _gen.Generate();
            pwd.All(c => allowed.Contains(c)).Should().BeTrue(
                $"char '{pwd.FirstOrDefault(c => !allowed.Contains(c))}' is outside the safe charset");
        }
    }

    [Fact]
    public void Generate_ThrowsWhenLengthIsLessThanThree()
    {
        Action act = () => _gen.Generate(2);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_ProducesDifferentValuesEachCall()
    {
        // Statistical check: 10 successive calls should not all collide.
        var passwords = Enumerable.Range(0, 10).Select(_ => _gen.Generate()).ToList();
        passwords.Distinct().Count().Should().BeGreaterThan(1,
            "RandomNumberGenerator-backed generator must produce non-constant output");
    }
}
