using UserManagement.Domain.Common;

namespace UserManagement.Domain.Entities;

// Entidad User del dominio: estado y comportamiento encapsulado (private set).
// Todos los cambios de estado se realizan a traves de metodos del dominio (Create, UpdateProfile, Lock, etc.).
// SecurityStamp = invalida todos los tokens JWT existentes al cambiar password.
// Email, FirstName, LastName: propiedades con getter publico y setter privado.
// Solo se modifican via metodos de dominio que aseguran invariantes.
public sealed class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string SecurityStamp { get; private set; } = Guid.NewGuid().ToString();
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? AvatarPath { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public string? EmailConfirmationToken { get; private set; }
    public DateTime? EmailConfirmationTokenExpires { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LastPasswordChangeAt { get; private set; }
    public int PasswordExpirationDays { get; private set; } = 30;
    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public bool LockoutEnabled { get; private set; } = true;

    public ICollection<UserRole> UserRoles { get; private set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    private User() { }

    public static User Create(string email, string firstName, string lastName, string passwordHash, Guid? createdBy = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            LastPasswordChangeAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string lastName, Guid? updatedBy = null)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        SecurityStamp = Guid.NewGuid().ToString();
        LastPasswordChangeAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AccessFailedCount = 0;
    }

    public void IncrementFailedAccess()
    {
        AccessFailedCount++;
    }

    public void LockUntil(DateTime until)
    {
        LockoutEnd = until;
    }

    public void Unlock()
    {
        LockoutEnd = null;
        AccessFailedCount = 0;
    }

    public void SetActive(bool active, Guid? updatedBy = null)
    {
        IsActive = active;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        EmailConfirmationToken = null;
        EmailConfirmationTokenExpires = null;
    }

    public void ForcePasswordChange()
    {
        LastPasswordChangeAt = null;
    }

    public void SetEmailConfirmationToken(string token, DateTime expires)
    {
        EmailConfirmationToken = token;
        EmailConfirmationTokenExpires = expires;
    }

    public void SetAvatar(string path)
    {
        AvatarPath = path;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsPasswordExpired()
    {
        if (LastPasswordChangeAt == null) return true;
        return DateTime.UtcNow > LastPasswordChangeAt.Value.AddDays(PasswordExpirationDays);
    }

    public bool IsLocked()
    {
        if (LockoutEnd == null) return false;
        return DateTime.UtcNow < LockoutEnd.Value;
    }
}
