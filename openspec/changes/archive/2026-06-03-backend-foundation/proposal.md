## Why

Establecer la base arquitectónica del sistema de gestión de usuarios: solución .NET 10 con arquitectura hexagonal, modelo de dominio rico, persistencia con EF Core + PostgreSQL, logging estructurado con Serilog, y contenedorización para desarrollo y producción.

## What Changes

- Creación de solución `AppBaseNetReact.slnx` con 4 proyectos: Domain, Application, Infrastructure, WebApi
- Implementación de 8 entidades de dominio: User, Role, Permission, RefreshToken, AuditLog, LoginAttempt, UserRole, RolePermission
- Configuración de EF Core 10 + PostgreSQL 18 con migración inicial (`InitialCreate`)
- Implementación de patrones: Repository genérico, UnitOfWork, BaseEntity con soft delete y concurrencia optimista
- Configuración de Serilog con sinks de Console y File (rolling daily)
- Pipeline de middleware: ExceptionHandling → SecurityHeaders → CORS → RateLimiter → Authentication → Authorization
- Dockerfile multi-stage para backend y frontend, docker-compose.yml con PostgreSQL 18 + Traefik
- Seed de datos inicial: roles (SuperAdmin, Admin, user-tipo-a/b/c), permisos granulares (18 permisos), usuario admin por defecto

## Capabilities

### New Capabilities

- `domain-entities`: Entidades del dominio con comportamiento encapsulado (factory methods, private setters), BaseEntity con audit fields, soft delete y concurrency token
- `data-persistence`: EF Core + PostgreSQL, AppDbContext con 8 DbSets, configuraciones de entidad, migración inicial, repositorio genérico y UnitOfWork
- `logging-monitoring`: Serilog estructurado con sinks de consola y archivo, request logging middleware, pipeline de middleware con exception handling global
- `container-infrastructure`: Dockerfiles multi-stage, docker-compose con PostgreSQL + backend + frontend + Traefik, nginx.conf para frontend SPA
- `seed-data`: DatabaseSeeder con roles del sistema, catálogo de permisos (18 permisos en 4 módulos), usuario SuperAdmin por defecto con contraseña temporal

### Modified Capabilities

Ninguna — es la implementación inicial.

## Impact

- Nuevos archivos: ~60 archivos entre Domain, Application, Infrastructure y WebApi
- Dependencias NuGet agregadas: Npgsql, Serilog, MediatR, FluentValidation, AutoMapper, Quartz.NET, MailKit
- Base de datos: PostgreSQL 18, migración inicial crea 8 tablas con índices únicos y filtrados
- Docker: imágenes .NET 10 SDK/Runtime y Node.js 22 Alpine + Nginx
