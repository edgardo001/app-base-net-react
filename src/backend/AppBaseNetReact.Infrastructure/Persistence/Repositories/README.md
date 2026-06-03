# Repositories — Implementación de Repositorios

Adaptadores que implementan los puertos definidos en `Application/Common/Interfaces/`.

| Clase | Puerto que implementa |
|-------|----------------------|
| `GenericRepository<T>` | `IRepository<T>` (CRUD genérico + paginación) |
| `UserRepository` | `IUserRepository` (búsqueda por email, con roles) |
| `RoleRepository` | `IRoleRepository` (búsqueda por nombre, con permisos) |
| `RefreshTokenRepository` | `IRefreshTokenRepository` (búsqueda por hash, revocación) |
| `AuditLogRepository` | `IAuditLogRepository` (historial por usuario) |
| `PermissionRepository` | `IPermissionRepository` (heredado de genérico) |
| `LoginAttemptRepository` | `ILoginAttemptRepository` (heredado de genérico) |

> Nota: Todos los repositorios están en un solo archivo `Repositories.cs`.
