## Why

El plan original (`planInicial.ia.md` sec. 14) define health checks completos (`/health/live`, `/health/ready`), un endpoint de métricas del sistema (`/api/admin/metrics`), y un dashboard de health en admin (`/api/admin/health`). Actualmente solo existe `/health` como endpoint inline. Sin estos endpoints no es posible monitorear la salud del sistema en producción ni implementar readiness probes en Docker/Traefik.

## What Changes

- **Health endpoints**: Agregar `/health/live` (liveness) y `/health/ready` (readiness con verificación de BD) usando `Microsoft.Extensions.Diagnostics.HealthChecks`
- **Admin metrics**: Nuevo endpoint `GET /api/admin/metrics` con métricas de proceso (uptime, memoria, requests)
- **Admin health**: Nuevo endpoint `GET /api/admin/health` con detalle de todos los checks del sistema

## Capabilities

### New Capabilities
- `health-checks`: Endpoints de liveness, readiness y startup para orquestación Docker y monitoreo
- `admin-metrics`: Endpoint admin con métricas del sistema en tiempo de ejecución

### Modified Capabilities
Ninguna

## Impact

- **Backend**: Nuevos endpoints en `AdminController`; configuración de `AddHealthChecks` + `MapHealthChecks` en `Program.cs`; dependencia NuGet `AspNetCore.HealthChecks`
- **Infraestructura**: Docker compose readiness probes pueden referenciar `/health/ready`
- **Tests**: Tests de integración para health checks y controller de metrics
