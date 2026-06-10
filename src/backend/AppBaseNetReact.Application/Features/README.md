# Features — CQRS por Feature

Estructura implementada con el patrón CQRS. Cada feature tiene su propia carpeta con `Commands/` y `Queries/`, handlers MediatR, validadores FluentValidation, y tipos Outcome.

| Feature | Propósito | Estado |
|---------|-----------|--------|
| `Auth/` | Login, refresh, logout, cambio de contraseña, forgot/reset password, confirm email | ✅ Migrado |
| `Users/` | CRUD de usuarios, activar/desactivar, reset password, revocar tokens, avatar | ✅ Migrado |
| `Roles/` | CRUD de roles, asignar permisos | 🏗️ Pendiente |
| `Permissions/` | Listar permisos agrupados por módulo | ✅ Migrado |
| `Profile/` | Ver/actualizar perfil, actividad reciente | ✅ Migrado |
| `Dashboard/` | Estadísticas del dashboard | 🏗️ Pendiente |
| `Audit/` | Log de auditoría global | 🏗️ Pendiente |

> ✅ **Auth:** 7 commands, 8 handlers, 11 notifications — migrado en `openspec/changes/cqrs-auth-*`.
> ✅ **Users:** 2 queries, 7 commands, 8 notifications — migrado en `openspec/changes/cqrs-users-management`.
> 🏗️ **Roles** es el siguiente módulo pendiente de migración.
