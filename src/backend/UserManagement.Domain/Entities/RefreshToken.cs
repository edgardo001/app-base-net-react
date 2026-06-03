using UserManagement.Domain.Common;

namespace UserManagement.Domain.Entities;

// RefreshToken implementa rotation + reuse detection:
// 1. Rotation: cada vez que se refresca un token, el anterior se revoca (RevokedAt) y se registra
//    el hash del reemplazo (ReplacedByTokenHash). El nuevo token se asocia al mismo JwtId.
// 2. Reuse detection: si un token ya revocado se presenta, se revocan TODOS los tokens del usuario
//    (sesion comprometida). Esto se detecta comparando ReplacedByTokenHash contra el hash presentado.
// 3. TokenHash: solo se almacena SHA-256 del token real. El token plano se devuelve al crear/refrescar.
// 4. JwtId (jti claim): correlaciona el refresh token con el access token actual.
// 5. RevokedBy: permite rastrear quien o que proceso revoco el token (admin, sistema, reuse detection).
public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid JwtId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, Guid jwtId, string tokenHash, DateTime expiresAt, string? deviceInfo, string? ipAddress)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JwtId = jwtId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Revoca el token y opcionalmente registra el hash del token que lo reemplaza.
    // replacedByTokenHash es el hash del nuevo refresh token (rotation).
    public void Revoke(Guid? revokedBy = null, string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
