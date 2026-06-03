# Services — Servicios de Infraestructura

Adaptadores que implementan los puertos de servicio definidos en `Application/Common/Interfaces/`.

| Clase | Puerto que implementa | Propósito |
|-------|-----------------------|-----------|
| `UnitOfWork` | `IUnitOfWork` | Agrupa todos los repositorios + `SaveChangesAsync`. Inicialización lazy (??=) para evitar constructor explosion |
| `DateTimeProvider` | `IDateTimeProvider` | Abstracción de `DateTime.UtcNow` |
| `AuditService` | `IAuditService` | Registro de operaciones en `AuditLog` (IP, User-Agent, acción, detalles) |
| `PasswordPolicyService` | `IPasswordPolicyService` | Reglas configurables: max failed attempts, lockout minutes, longitud mínima de contraseña |
| `DatabaseSeeder` | — (no es puerto) | Seed inicial: 22 permisos, 5 roles, usuario admin |
