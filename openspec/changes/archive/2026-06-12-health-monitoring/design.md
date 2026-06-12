## Context

Actualmente solo existe un endpoint `/health` inline en `Program.cs` que responde "Healthy". No hay health checks de infraestructura (BD, dependencias), no hay endpoints diferenciados de liveness/readiness, y no hay endpoint admin de métricas ni health dashboard. El plan original (sec. 14) define 3 health endpoints + admin metrics.

## Goals / Non-Goals

**Goals:**
- Agregar `/health/live` (siempre 200 si el proceso está vivo)
- Agregar `/health/ready` (200 solo si BD responde + migraciones aplicadas)
- Agregar `GET /api/admin/health` con detalle de todos los checks individuales
- Agregar `GET /api/admin/metrics` con uptime, memoria, total requests, etc.
- Usar `Microsoft.Extensions.Diagnostics.HealthChecks` del propio ASP.NET Core 10

**Non-Goals:**
- No se agrega Prometheus/Grafana (Post-MVP)
- No se agregan health checks de servicios externos (SMTP, Redis, etc.)
- No se agregan métricas avanzadas (request duration percentiles, etc.)

## Decisions

### Usar Health Checks built-in de ASP.NET Core
- **Decisión**: `AddHealthChecks()` + `MapHealthChecks("/health/ready")` con filtro de puerto
- **Rationale**: Ya incluido en ASP.NET Core 10 sin dependencias externas. El `/health` actual inline se reemplaza
- **Alternativa**: NuGet `AspNetCore.HealthChecks.*` con chequeo de PostgreSQL — más features pero más dependencias

### Métricas ligeras vs Prometheus
- **Decisión**: Endpoint `GET /api/admin/metrics` que expone métricas básicas de proceso (Process.GetCurrentProcess, GC.GetTotalMemory, Environment.TickCount)
- **Rationale**: Suficiente para monitoreo operativo sin agregar dependencias. Prometheus queda como mejora Post-MVP
- **Alternativa**: Prometheus + `dotnet-counters` — sobre ingeniería para el MVP

## Risks / Trade-offs

- [Risk] Health checks de BD pueden causar timeouts si la BD está lenta → Mitigación: timeout corto (3s) y cache短暂 del resultado (HealthCheckService)
- [Risk] Endpoint de métricas expone información del proceso → Mitigación: solo accesible vía JWT con rol Admin
- [Trade-off] Sin health checks de SMTP: el monitoreo de correos queda cubierto por el cambio `email-background-queue`
