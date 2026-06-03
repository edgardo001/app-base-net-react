## Context

Los controladores de usuarios, roles, permisos, perfil y administración implementan la capa de presentación de la API REST. Siguen el patrón de inyección directa de IUnitOfWork (sin CQRS commands/queries) por pragmatismo — los handlers de MediatR no están implementados.

## Goals / Non-Goals

**Goals:**
- CRUD completo de usuarios con operaciones administrativas
- Gestión de roles con permisos y protección de roles de sistema
- Catálogo de permisos read-only para UI de asignación
- Self-service para perfil propio
- Dashboard admin con métricas y auditoría
- ApiResponse<T> como formato único de respuesta

**Non-Goals:**
- Avatar upload (endpoints planificados pero no implementados)
- Import/Export CSV (post-MVP)
- CQRS commands/queries (arquitectura planificada pero no implementada a nivel de handlers)

## Decisions

### Inyección directa de IUnitOfWork vs CQRS
**Decisión:** Los controladores inyectan IUnitOfWork y llaman a repositorios directamente.
**Alternativa:** Commands/Queries con MediatR handlers separados.
**Razón:** Pragmatismo. El proyecto tiene MediatR registrado y el pipeline de validación configurado, pero implementar handlers separados para cada operación CRUD añade ~200 archivos sin beneficio inmediato. Se puede migrar progresivamente a CQRS a medida que la lógica de negocio se vuelva más compleja.

### Protección de roles de sistema en controlador vs dominio
**Decisión:** La validación de IsSystem para evitar modificación/eliminación se hace en el controlador.
**Razón:** El dominio expone IsSystem como propiedad de solo lectura. La lógica de "no modificar si es sistema" es una regla de aplicación, no de dominio. El controlador devuelve 422 con mensaje específico.

### DTOs inline vs archivos separados
**Decisión:** DTOs definidos en los mismos archivos de controlador o en validators compartidos.
**Razón:** Los DTOs son específicos de cada endpoint y están estrechamente acoplados a la request/response del controlador. Separarlos en archivos individuales añade ruido sin beneficio.

### ApiResponse<T> sin filtro global
**Decisión:** ApiResponse<T> se usa explícitamente en cada controller (Ok(ApiResponse<object>.Ok(data))) en lugar de un filtro global automático.
**Razón:** Control explícito sobre cada respuesta. Algunos endpoints pueden necesitar formatos diferentes (descargas, streaming). Un filtro global complicaría estos casos.

## Risks / Trade-offs

- **Riesgo: Crecimiento de controladores** → Sin CQRS, los controladores pueden crecer demasiado. Mitigación: dividir en controladores más pequeños por recurso (ya hecho), migrar a CQRS cuando sea necesario.
- **Riesgo: Sin validación automática de permisos** → No hay un middleware de autorización por permiso (solo por rol). Mitigación: los permisos se incluyen en el JWT para que el frontend pueda hacer UI conditional.
- **Riesgo: DTOs duplicados** → Algunos DTOs pueden duplicarse entre controladores. Mitigación: mover DTOs compartidos a Application/Common cuando sea necesario.
