# Entities — Entidades del Negocio

Entidades con comportamiento encapsulado (private set + métodos públicos). Son el núcleo del dominio.

| Archivo | Descripción |
|---------|-------------|
| `User.cs` | Agregado raíz. Métodos: `Create()`, `MarkLogin()`, `LockUntil()`, `SetPasswordHash()`, `IncrementFailedAccess()`, `IsLocked()`, `ChangePassword()`, `Activate()`, `Deactivate()`, `ConfirmEmail()`, `SetMustChangePassword()` |
| `Role.cs` | Agregado raíz con colección `RolePermissions`. Métodos: `Create()`, `Update()`, `AddPermission()`, `RemovePermission()` |
| `Permission.cs` | Permiso plano (Name, Module, Description). Factory: `Create()` |
| `RefreshToken.cs` | Token de refresco con rotación. Métodos: `Create()`, `Revoke()`, `MarkAsReused()` |
| `AuditLog.cs` | Registro de auditoría inmutable. Factory: `Create()` con IP, User-Agent, acción, detalles |
| `LoginAttempt.cs` | Intento de login. Factory: `Create()` con email, IP, resultado |
| `UserRole.cs` | Join User-Role (UserId, RoleId) |
| `RolePermission.cs` | Join Role-Permission (RoleId, PermissionId) |

## Reglas

- Solo se modifican mediante métodos de comportamiento (nunca setters públicos)
- Constructores privados + factory estáticos
- `User`, `Role` son agregados raíz con entidades hijo (UserRole, RolePermission)
