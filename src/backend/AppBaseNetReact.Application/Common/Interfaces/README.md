# Interfaces — Puertos (Ports)

Contratos que define la capa de aplicación y que `Infrastructure` debe implementar (adapters).

## Repositorios

| Interfaz | Métodos clave |
|----------|--------------|
| `IRepository<T>` | CRUD genérico + paginación (`GetPagedAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Delete`, `ExistsAsync`) |
| `IUserRepository` | `GetByEmailAsync`, `GetByIdWithRolesAsync`, `GetUsersByRoleAsync` |
| `IRoleRepository` | `GetByNameAsync`, `GetByIdWithPermissionsAsync` |
| `IRefreshTokenRepository` | `GetByTokenHashAsync`, `GetActiveByUserAsync`, `RevokeAllForUserAsync`, `RevokeAllGlobalAsync` |
| `IAuditLogRepository` | `GetByUserAsync` |
| `IPermissionRepository` | Hereda de `IRepository<Permission>` (métodos genéricos) |
| `ILoginAttemptRepository` | Hereda de `IRepository<LoginAttempt>` |

## Unidad de Trabajo

| Interfaz | Propósito |
|----------|-----------|
| `IUnitOfWork` | Expone todos los repositorios como propiedades + `SaveChangesAsync()` |

## Servicios

| Interfaz | Propósito |
|----------|-----------|
| `IJwtService` | Generar access token (HS512), generar/hash/validar refresh token |
| `IPasswordHasherService` | Hash y verificación de contraseñas (PBKDF2) |
| `IEmailService` | Envío de correos transaccionales |
| `ICaptchaService` | Verificación de Cloudflare Turnstile |
| `IDateTimeProvider` | Abstracción de `DateTime.UtcNow` (testeable) |
| `IAuditService` | Registro de operaciones en AuditLog |
| `IPasswordPolicyService` | Reglas de contraseñas (intentos máximos, lockout, etc.) |
