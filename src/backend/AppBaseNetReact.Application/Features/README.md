# Features — CQRS por Feature

Estructura preparada para el patrón CQRS. Cada feature tiene su propia carpeta con `Commands/` y `Queries/`.

| Feature | Propósito | Estado |
|---------|-----------|--------|
| `Auth/` | Login, refresh, logout, cambio de contraseña, forgot/reset password | 🏗️ Pendiente |
| `Users/` | CRUD de usuarios, activar/desactivar, reset password, revocar tokens | 🏗️ Pendiente |
| `Roles/` | CRUD de roles, asignar permisos | 🏗️ Pendiente |
| `Permissions/` | Listar permisos agrupados por módulo | 🏗️ Pendiente |
| `Profile/` | Ver/actualizar perfil, actividad reciente | 🏗️ Pendiente |
| `Dashboard/` | Estadísticas del dashboard | 🏗️ Pendiente |
| `Audit/` | Log de auditoría global | 🏗️ Pendiente |

> ⚡ **Estado actual:** Las carpetas `Commands/` y `Queries/` existen pero están vacías. La lógica se ejecuta directamente en los controladores de `WebApi`. Pendiente migrar a handlers CQRS.
