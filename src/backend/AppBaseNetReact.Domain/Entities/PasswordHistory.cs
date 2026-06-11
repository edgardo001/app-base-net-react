using AppBaseNetReact.Domain.Common;

namespace AppBaseNetReact.Domain.Entities;

public sealed class PasswordHistory : BaseEntity
{
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public User User { get; private set; } = null!;

    private PasswordHistory() { }

    public static PasswordHistory Create(Guid userId, string passwordHash)
    {
        return new PasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash
        };
    }
}
