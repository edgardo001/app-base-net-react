# AppBaseNetReact.Domain — Núcleo del Negocio

## Propósito

Capa más interna de la arquitectura hexagonal. Contiene las **entidades del negocio** con su comportamiento, sin dependencias externas (solo `MediatR.Contracts` para futuros eventos de dominio).

## Dependencias

- **Referencia:** Ninguna (0 `ProjectReference`)
- **Referenciado por:** `Application`, `Infrastructure`

## Estructura

```
Common/
  BaseEntity.cs              — Clase base abstracta con Id (Guid), CreatedAt/UpdatedAt,
                               DeletedAt (soft-delete), ConcurrencyToken (row version)
Entities/
  User.cs                    — Agregado raíz con métodos: Create, MarkLogin, LockUntil,
                               SetPasswordHash, IncrementFailedAccess, ChangePassword, etc.
  Role.cs                    — Agregado raíz con colección RolePermissions
  Permission.cs              — Entidad permiso (idempotente, por nombre)
  RefreshToken.cs            — Token de refresco con rotación y detección de reuso
  AuditLog.cs                — Registro de auditoría inmutable
  LoginAttempt.cs            — Intento de login (fallido/exitoso)
  UserRole.cs                — Join con riqueza (usuario ↔ rol)
  RolePermission.cs          — Join con riqueza (rol ↔ permiso)
Enums/                       — Espacio reservado para enumeraciones del dominio
ValueObjects/                — Espacio reservado para objetos valor
```

## Convenciones

- Entidades con `private set` — solo se modifican mediante métodos de comportamiento
- Factory estáticos: `User.Create(...)`, `Role.Create(...)`, `Permission.Create(...)`
- `BaseEntity` provee auditoría y soft-delete a todas las entidades

> ⚠️ **No agregar dependencias externas** — esta capa debe permanecer pura.
