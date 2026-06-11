## 1. Baseline — Regla de Oro

- [x] 1.1 Run `dotnet test` — confirm 247/247 green (actually 392/392 from prior changes)
- [x] 1.2 Verify current `/health` endpoint in `Program.cs` lines 102-116

## 2. Health checks — Backend

- [x] 2.1 Add `AddHealthChecks()` to `Program.cs` service collection
- [x] 2.2 Configure DB health check: `AddDbContextCheck<AppDbContext>()` with 3s timeout (nuget: Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore)
- [x] 2.3 Map `/health/live` — always returns Healthy (just liveness)
- [x] 2.4 Map `/health/ready` — runs DB check, returns Healthy/Unhealthy
- [x] 2.5 Remove existing inline `/health` endpoint, keep as simple `Results.Ok({status: "Healthy"})` alias
- [x] 2.6 Run `dotnet build` — confirm 0 errors

## 3. Admin health endpoint

- [x] 3.1 Add `GET /api/admin/health` to `AdminController` — resolves `HealthCheckService` from `HttpContext.RequestServices` and returns detailed report
- [x] 3.2 Protected by class-level `[Authorize(Roles = "SuperAdmin,Admin")]` (HasPermission attribute no existe en codebase)
- [x] 3.3 Add test in `AdminControllerTests` — returns detailed JSON (GetHealth_ReturnsDetailedReport)

## 4. Admin metrics endpoint

- [x] 4.1 Create `Application/Features/Admin/Queries/GetMetrics/GetMetricsQuery.cs` + `GetMetricsQueryHandler.cs`
- [x] 4.2 Handler returns: uptime, memory (GC.GetTotalMemory), GC collections per gen, thread pool thread count
- [x] 4.3 Add `GET /api/admin/metrics` to `AdminController` delegating to MediatR
- [x] 4.4 Protected by class-level `[Authorize(Roles = "SuperAdmin,Admin")]`
- [x] 4.5 Add tests in `AdminControllerTests` (GetMetrics_ReturnsSystemMetrics)

## 5. Final validation

- [x] 5.1 Run `dotnet build` — 0 errors
- [x] 5.2 Run `dotnet test` — 394/394 pass (256 Application + 138 WebApi)
- [ ] 5.3 Verify `/health/live` returns 200, `/health/ready` returns 200 (DB up), or 503 (DB down) — requires running backend with DB
