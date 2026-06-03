# Identity — Autenticación JWT y Hashing

Adaptadores para servicios de identidad.

| Archivo | Propósito |
|---------|-----------|
| `JwtService.cs` | Implementa `IJwtService` — Generación de access token (HS512, 15 min), refresh token (7 días), hash SHA-256 + validación con `FixedTimeEquals` |

## Características de JwtService

- Access token con claims: sub, email, name, roles, permissions, nbf, iat
- Refresh token con rotación (cada refresh invalida el anterior)
- Detección de reuso: si un token ya revocado se usa, revoca toda la familia
- `FixedTimeEquals` para comparación time-constant de hashes
