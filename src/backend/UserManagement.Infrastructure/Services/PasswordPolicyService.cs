using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Interfaces;

namespace UserManagement.Infrastructure.Services;

public class PasswordPolicyService : IPasswordPolicyService
{
    private readonly PasswordPolicySettings _settings;

    public PasswordPolicyService(IOptions<PasswordPolicySettings> settings)
    {
        _settings = settings.Value;
    }

    public int RequiredLength => _settings.RequiredLength;
    public bool RequireNonAlphanumeric => _settings.RequireNonAlphanumeric;
    public bool RequireLowercase => _settings.RequireLowercase;
    public bool RequireUppercase => _settings.RequireUppercase;
    public bool RequireDigit => _settings.RequireDigit;
    public int MaxFailedAccessAttempts => _settings.MaxFailedAccessAttempts;
    public int DefaultLockoutMinutes => _settings.DefaultLockoutMinutes;
    public int ExpirationDays => _settings.ExpirationDays;

    public (bool Valid, string Error) Validate(string password)
    {
        if (password.Length < _settings.RequiredLength)
            return (false, $"Password must be at least {_settings.RequiredLength} characters long");

        if (_settings.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            return (false, "Password must contain at least one non-alphanumeric character");

        if (_settings.RequireLowercase && !password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter");

        if (_settings.RequireUppercase && !password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter");

        if (_settings.RequireDigit && !password.Any(char.IsDigit))
            return (false, "Password must contain at least one digit");

        return (true, string.Empty);
    }
}

public class PasswordPolicySettings
{
    public int RequiredLength { get; set; } = 10;
    public int RequiredUniqueChars { get; set; } = 4;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public int ExpirationDays { get; set; } = 30;
    public int PasswordHistoryCount { get; set; } = 5;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int DefaultLockoutMinutes { get; set; } = 15;
}
