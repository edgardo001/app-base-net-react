namespace AppBaseNetReact.Application.Common.Interfaces;

public interface IPasswordPolicyService
{
    (bool Valid, string Error) Validate(string password);
    int RequiredLength { get; }
    bool RequireNonAlphanumeric { get; }
    bool RequireLowercase { get; }
    bool RequireUppercase { get; }
    bool RequireDigit { get; }
    int MaxFailedAccessAttempts { get; }
    int DefaultLockoutMinutes { get; }
    int ExpirationDays { get; }
}
