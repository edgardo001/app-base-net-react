using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagement.Application.Common.Interfaces;
using UserManagement.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace UserManagement.Infrastructure.Identity;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    // HS512 (HMAC-SHA512): elegido sobre RS256 por simplicidad operativa (sin gestion de certificados).
    // La clave debe tener al menos 64 bytes (512 bits) para cumplir con el algoritmo.
    // Access token de 15 min: ventana de exposicion limitada si el token es comprometido.
    // Claims: sub = UserId, email = Email, permission = lista plana de permisos del usuario.
    public (string accessToken, DateTime expiresAt) GenerateAccessToken(User user, IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // Refresh token: 64 bytes criptograficamente aleatorios (RNGCryptoServiceProvider).
    // Se almacena SOLO el hash SHA-256 en la DB. El token plano se devuelve al cliente una unica vez.
    // FixedTimeEquals: previene timing attacks al comparar hashes.
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // SHA-256 hash antes de almacenar: mismo approach que ASP.NET Core Identity PersonalData protection.
    public string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    // CryptographicOperations.FixedTimeEquals: comparacion en tiempo constante para evitar timing attacks.
    // Sin FixedTimeEquals, un atacante podria deducir el hash correcto midiendo el tiempo de respuesta.
    public bool ValidateRefreshToken(string refreshToken, string tokenHash)
    {
        var hash = HashRefreshToken(refreshToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(tokenHash));
    }
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int ClockSkewSeconds { get; set; } = 60;
}

public class PasswordHasherService : IPasswordHasherService
{
    private const int SaltSize = 128 / 8;
    private const int KeySize = 256 / 8;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    // PBKDF2 con SHA-256: estandar NIST para derivacion de claves.
    // Salt unico por password (128 bits): previene rainbow table attacks.
    // 100,000 iteraciones: balance entre seguridad y rendimiento (OWASP recommendation 2024).
    // Formato almacenado: "{salt}.{hash}" en Base64.
    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}
