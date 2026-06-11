using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Infrastructure.Services;

public class PasswordPolicyService : IPasswordPolicyService
{
    private readonly PasswordPolicySettings _settings;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasherService _hasher;

    public PasswordPolicyService(IOptions<PasswordPolicySettings> settings, IUnitOfWork uow, IPasswordHasherService hasher)
    {
        _settings = settings.Value;
        _uow = uow;
        _hasher = hasher;
    }

    public int RequiredLength => _settings.RequiredLength;
    public bool RequireNonAlphanumeric => _settings.RequireNonAlphanumeric;
    public bool RequireLowercase => _settings.RequireLowercase;
    public bool RequireUppercase => _settings.RequireUppercase;
    public bool RequireDigit => _settings.RequireDigit;
    public int MaxFailedAccessAttempts => _settings.MaxFailedAccessAttempts;
    public int DefaultLockoutMinutes => _settings.DefaultLockoutMinutes;
    public int ExpirationDays => _settings.ExpirationDays;
    public int PasswordHistoryCount => _settings.PasswordHistoryCount;

    public async Task<bool> CheckPasswordHistoryAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        if (_settings.PasswordHistoryCount <= 0) return true;

        var recentHashes = await _uow.PasswordHistories
            .GetRecentHashesAsync(userId, _settings.PasswordHistoryCount, ct);

        foreach (var hash in recentHashes)
        {
            if (_hasher.VerifyPassword(newPassword, hash))
                return false;
        }

        return true;
    }

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
