# Configurations — Configuración de Entidades EF Core

Contiene las clases `IEntityTypeConfiguration<T>` que definen el mapeo de cada entidad a tablas de PostgreSQL.

| Archivo | Entidad |
|---------|---------|
| `EntityConfigurations.cs` | Contiene todas las configuraciones: UserConfig, RoleConfig, PermissionConfig, RefreshTokenConfig, AuditLogConfig, LoginAttemptConfig, UserRoleConfig, RolePermissionConfig |

Incluye:
- Nombres de tabla y columna
- Llaves primarias, índices, unique constraints
- Relaciones (FK, cascade behavior)
- Conversiones (ej: `List<Permission>` a JSON)
- Seed data inicial en `OnDataSeeding`
