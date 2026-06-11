using AppBaseNetReact.Domain.Common;

namespace AppBaseNetReact.Domain.Entities;

public sealed class ExternalLogin : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderId { get; private set; } = string.Empty;
    public string ProviderEmail { get; private set; } = string.Empty;

    public User User { get; private set; } = null!;

    private ExternalLogin() { }

    public static ExternalLogin Create(Guid userId, string provider, string providerId, string providerEmail)
    {
        return new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderId = providerId,
            ProviderEmail = providerEmail,
            CreatedAt = DateTime.UtcNow
        };
    }
}
