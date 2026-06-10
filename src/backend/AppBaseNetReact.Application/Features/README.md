# Features — CQRS por Feature

Estructura implementada con el patrón CQRS. Cada feature tiene su propia carpeta con `Commands/` y `Queries/`, handlers MediatR, validadores FluentValidation, y tipos Outcome.

| Feature | Propósito | Estado |
|---------|-----------|--------|
| `Auth/` | Login, refresh, logout, cambio de contraseña, forgot/reset password, confirm email | ✅ Migrado |
| `Users/` | CRUD de usuarios, activar/desactivar, reset password, revocar tokens, avatar | ✅ Migrado |
| `Roles/` | CRUD de roles, asignar permisos | ✅ Migrado |
| `Permissions/` | Listar permisos agrupados por módulo | ✅ Migrado |
| `Profile/` | Ver/actualizar perfil, actividad reciente | ✅ Migrado |
| `Admin/` | Dashboard, auditoría, revocar tokens, test email | ✅ Migrado |

> Todos los módulos migrados a CQRS. Dashboard y Audit viven dentro de `Admin/` feature.
