## Context

Sistema de gestión de usuarios enterprise con arquitectura hexagonal (puertos y adaptadores). El dominio es el centro sin dependencias externas. La infraestructura implementa los puertos definidos en Application. WebApi orquesta la configuración de startup.

Se decidió .NET 10 por ser la versión LTS más reciente con soporte a largo plazo. PostgreSQL 18 como base de datos relacional por su madurez, performance y capacidades empresariales.

## Goals / Non-Goals

**Goals:**
- Establecer estructura de proyecto escalable con separación clara de capas
- Modelo de dominio rico con comportamiento encapsulado (no anémico)
- Persistencia con EF Core 10 + PostgreSQL 18 con migraciones code-first
- Logging estructurado para depuración y monitoreo
- Contenedorización completa para desarrollo y producción
- Seed de datos inicial para desarrollo y pruebas

**Non-Goals:**
- Implementar lógica de negocio específica de features (auth, CRUD)
- Configurar CI/CD
- Implementar pruebas automáticas

## Decisions

### Arquitectura Hexagonal con referencias pragmáticas
**Decisión:** WebApi referencia tanto Application como Infrastructure.
**Alternativa considerada:** WebApi conociendo solo Application y usando DI para resolver Infrastructure.
**Razón:** En .NET, Infrastructure necesita ser referenciada por WebApi para registrar sus servicios en el contenedor DI (AddInfrastructure). Aislar Infrastructure requeriría un proyecto Composition Root separado, lo que añade complejidad sin beneficio significativo para este tamaño de proyecto.

### Domain sin dependencias externas
**Decisión:** El proyecto Domain solo referencia `MediatR.Contracts` para domain events futuros.
**Razón:** El dominio debe ser puramente modelo de negocio, sin acoplamiento a frameworks de persistencia o infraestructura.

### GUID como PK vs auto-increment
**Decisión:** GUID para todas las entidades.
**Alternativa considerada:** int auto-increment.
**Razón:** Evita roundtrips a DB para generar IDs, previene enumeración de recursos via IDs secuenciales, facilita migraciones y réplicas.

### Soft delete con BaseEntity
**Decisión:** `DeletedAt` nullable en BaseEntity para todas las entidades.
**Razón:** Cumplimiento GDPR, auditoría completa, recuperación de datos, sin pérdida de información. Global query filters excluyen automáticamente registros eliminados.

### ConcurrencyToken con byte[]
**Decisión:** EF Core optimistic concurrency via `ConcurrencyToken` byte array.
**Razón:** Previene lost updates sin necesidad de locks pesados. El byte array es más eficiente que usar timestamp.

### Serilog vs ILogger nativo
**Decisión:** Serilog con Console + File sinks.
**Alternativa considerada:** Solo ILogger de Microsoft.
**Razón:** Logging estructurado con mejor formato, soporte para múltiples sinks, enriquecimiento de contexto, y fácil integración con sistemas externos (ELK, Datadog, etc.).

### Docker multi-stage vs single-stage
**Decisión:** Multi-stage builds para backend y frontend.
**Razón:** Imágenes de producción ~100x más pequeñas (ASP.NET runtime sin SDK), menor superficie de ataque, builds reproducibles.

## Risks / Trade-offs

- **Riesgo: EF Core lazy loading** → No usamos lazy loading. Todas las relaciones se cargan explícitamente via Include/ThenInclude.
- **Riesgo: GUID como clustered index** → PostgreSQL no tiene clustered indexes nativos, las tablas heap no sufren fragmentación como SQL Server con GUIDs.
- **Riesgo: Soft delete en todas las entidades** → Aumenta complejidad en queries (siempre filtrar DeletedAt == null). Mitigación: EF Core global query filter abstracto en BaseEntity configurado centralmente.
- **Riesgo: Seed data en producción** → El seeder verifica si ya existen datos antes de insertar. En producción se debe deshabilitar explícitamente.
