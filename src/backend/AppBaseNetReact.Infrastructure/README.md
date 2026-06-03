# AppBaseNetReact.Infrastructure — Adaptadores (Implementaciones)

## Propósito

Capa más externa que **implementa los puertos** definidos en `Application`. Contiene toda la infraestructura técnica: persistencia EF Core, JWT, hashing, email, etc.

## Dependencias

- **Referencia:** `Application` (para implementar sus interfaces), `Domain`
- **Referenciado por:** `WebApi`
- **Paquetes NuGet:** EF Core (Npgsql/PostgreSQL), JWT (System.IdentityModel.Tokens), etc.

## Estructura

```
Persistence/
  AppDbContext.cs              — DbContext de EF Core con SaveChangesAsync override (auditoría automática)
  AppDbContextFactory.cs       — Factory para CLI de EF Core (migrations)
  Configurations/
    EntityConfigurations.cs    — IEntityTypeConfiguration para cada entidad (UserConfig, RoleConfig, etc.)
  Repositories/
    Repositories.cs            — GenericRepository<T> + UserRepository, RoleRepository,
                                 RefreshTokenRepository, AuditLogRepository, PermissionRepository,
                                 LoginAttemptRepository
  Migrations/                  — Migraciones de base de datos generadas por EF Core
Identity/
  JwtService.cs                — JWT HS512 con generación de access/refresh tokens
Services/
  UnitOfWork.cs                — IUnitOfWork + DateTimeProvider
  AuditService.cs              — IAuditService (registro de operaciones en AuditLog)
  PasswordPolicyService.cs     — IPasswordPolicyService (reglas de contraseñas configurable)
  DatabaseSeeder.cs            — Seed de datos iniciales (roles, permisos, admin)
Email/                         — Espacio reservado para implementación de envío de correos
DependencyInjection.cs         — RegisterServices(): DbContext, JWT Auth, repos, UnitOfWork, servicios
```

## Principios

- **Adaptadores:** Cada clase implementa una interfaz definida en `Application/Common/Interfaces/`
- **UnitOfWork:** Expone repositorios como propiedades lazy; evita constructor explosion en controllers
- **JWT:** Access token corto (15 min) + refresh token largo (7 días) con rotación y detección de reuso

> 🔌 **Contratos que implementa**: `IRepository<T>`, `IUserRepository`, `IRoleRepository`, `IUnitOfWork`, `IJwtService`, `IPasswordHasherService`, `IAuditService`, `IPasswordPolicyService`, `IDateTimeProvider`
