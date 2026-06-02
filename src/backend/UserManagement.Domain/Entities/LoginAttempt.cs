using UserManagement.Domain.Common;

namespace UserManagement.Domain.Entities;

public sealed class LoginAttempt : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string? FailureReason { get; private set; }

    private LoginAttempt() { }

    public static LoginAttempt Create(string email, string ipAddress, bool success, string? failureReason = null)
    {
        return new LoginAttempt
        {
            Id = Guid.NewGuid(),
            Email = email,
            IpAddress = ipAddress,
            Success = success,
            FailureReason = failureReason,
            CreatedAt = DateTime.UtcNow
        };
    }
}
