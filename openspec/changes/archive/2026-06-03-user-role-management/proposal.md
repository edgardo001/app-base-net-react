## Why

Implementar la gestión completa de usuarios, roles y permisos del sistema con RBAC (Role-Based Access Control). El planInicial.ia.md define 5 roles iniciales (SuperAdmin, Admin, user-tipo-a/b/c), 18 permisos granulares organizados en módulos, endpoints CRUD con paginación server-side, y auditoría completa de todas las operaciones.

## What Changes

- UsersController con CRUD completo: listar (paginado, ordenable, buscable), crear, actualizar, eliminar (soft), activar/desactivar, resetear contraseña, revocar tokens
- RolesController con CRUD: listar, detalle con permisos, crear, actualizar, eliminar (protegido para roles de sistema), actualizar permisos
- PermissionsController read-only: listar todos, agrupar por módulo
- ProfileController: perfil propio, actividad reciente, actualizar perfil
- AdminController: dashboard con métricas, audit-log paginado, revocación global de tokens
- Autorización por roles ([Authorize(Roles = "SuperAdmin")]) en endpoints administrativos
- DTOs específicos: UserDto, UserDetailDto, RoleDto, RoleDetailDto, PagedResponse<T>
- ApiResponse<T> wrapper estandarizado para todas las respuestas
- Filtro ApiResponseFilter para envolver respuestas automáticamente

## Capabilities

### New Capabilities

- `users-crud`: CRUD completo de usuarios con paginación server-side, búsqueda, ordenamiento, soft delete, activación/desactivación
- `roles-crud`: CRUD de roles con protección de roles de sistema, asignación de permisos
- `permissions-catalog`: Catálogo de permisos read-only, agrupado por módulo
- `self-service-profile`: Perfil de usuario autenticado (ver/editar nombre, actividad)
- `admin-dashboard`: Dashboard con métricas (totales, activos, nuevos, bloqueados), audit-log paginado, revocación global
- `rbac-authorization`: Control de acceso basado en roles con permisos granulares
- `api-response-standardization`: Wrapper ApiResponse<T> estandarizado para todas las respuestas API

### Modified Capabilities

Ninguna.

## Impact

- 6 controladores nuevos: UsersController, RolesController, PermissionsController, ProfileController, AdminController
- Autorización: [Authorize(Roles = "...")] en AdminController, [Authorize] en el resto
- DTOs en cada controlador o en validadores compartidos
- Seed data existente asigna permisos por rol según planInicial.ia.md
