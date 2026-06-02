namespace UserManagement.Application.Common.Interfaces;

public interface IJwtService
{
    (string accessToken, DateTime expiresAt) GenerateAccessToken(Domain.Entities.User user, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    bool ValidateRefreshToken(string refreshToken, string tokenHash);
}

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public interface ICaptchaService
{
    bool IsEnabled { get; }
    Task<bool> VerifyTokenAsync(string token, CancellationToken ct = default);
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
