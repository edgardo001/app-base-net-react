# Plan Integral — Sistema de Gestión de Usuarios Enterprise

## Estado Actual de Implementación (02-Jun-2026)

### ✅ Completado

#### Backend — Capa de Infraestructura
- [x] DbContext con todas las entidades (User, Role, Permission, RolePermission, RefreshToken, AuditLog, LoginAttempt)
- [x] Configuraciones EF Core (índices, relaciones, query filters para soft-delete)
- [x] GenericRepository<T> con paginación, ordenamiento, búsqueda
- [x] Repositorios: UserRepository, RoleRepository, RefreshTokenRepository, AuditLogRepository, PermissionRepository, LoginAttemptRepository
- [x] UnitOfWork con todos los repositorios
- [x] PasswordPolicyService validación (longitud, mayúsculas, minúsculas, dígitos, especiales)
- [x] AuditService para logging de operaciones críticas

#### Backend — Capa de Aplicación
- [x] Interfaces: IUnitOfWork, IUserRepository, IRoleRepository, IRefreshTokenRepository, IAuditLogRepository, IPermissionRepository, ILoginAttemptRepository, IPasswordPolicyService, IAuditService
- [x] DTOs: LoginRequest, RefreshRequest, ChangePasswordRequest, ForgotPasswordRequest, UpdateProfileRequest
- [x] Validadores FluentValidation para todos los DTOs
- [x] PagedResult<T> genérico

#### Backend — WebApi
- [x] **AuthController**: POST login, refresh, logout, change-password, forgot-password
  - [x] Lockout automático tras N intentos fallidos (423 Locked)
  - [x] Rate limiting (Login: 10/min, ForgotPassword: 3/hora, Global: 100/min)
  - [x] Auditoría en cada operación
  - [x] Refresh token rotation
- [x] **UsersController**: CRUD completo + toggle active + reset password
- [x] **RolesController**: CRUD + asignación de permisos
- [x] **PermissionsController**: GET permissions, GET permissions/modules
- [x] **ProfileController**: GET /, PUT /, GET /activity
- [x] **AdminController**: GET dashboard, GET audit-log (paginado), POST revoke-all-tokens
- [x] SecurityHeadersMiddleware (CSP, X-Frame-Options, X-Content-Type-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy)
- [x] ApiResponse<T> wrapper para todas las respuestas
- [x] Pipeline de middleware: Rate Limiting → Security Headers → CORS → Auth → Endpoints
- [x] appsettings.json + appsettings.Development.json con todas las secciones (JWT, PasswordPolicy, RateLimiting, Email, Storage, Session, Captcha, CORS, Serilog)

#### Frontend
- [x] Setup: React 19 + Vite + TypeScript + Tailwind v4 + shadcn/ui
- [x] **Auth**: Login page, auth-store (Zustand), JWT interceptor, refresh token
- [x] **Layout**: Sidebar colapsable (persiste en localStorage) + Header + main content
- [x] **SessionWarning**: Modal countdown 30s antes de expirar JWT, refresh o logout
- [x] **Dashboard**: Cards métricas (total, activos, nuevos 7d, inactivos) + actividad reciente
- [x] **Users**: CRUD tabla paginada, crear/editar modal, toggle active, soft-delete, reset password
- [x] **Roles**: CRUD cards, crear/editar modal con asignación de permisos por módulo, delete con protección de roles de sistema
- [x] **Profile**: Editar nombre, cambiar contraseña con verificación, historial de actividad
- [x] **Admin**: Visor de auditoría paginado, botón revocar todas las sesiones
- [x] **Permissions**: Listado de permisos (página simple)
- [x] Loading states, empty states, error handling en todas las páginas

#### Testing
- [x] Proyecto: `tests/UserManagement.Application.Tests` (xUnit + Moq + FluentAssertions)
  - [x] PasswordPolicyServiceTests (7 tests)
  - [x] UserTests (6 tests: Create, UpdateProfile, MarkLogin, IncrementFailedAccess, SetPasswordHash, SoftDelete)
- [x] Proyecto: `tests/UserManagement.WebApi.Tests` (xUnit + Moq + FluentAssertions)
  - [x] ProfileControllerTests (4 tests: GetProfile ok, GetProfile not found, UpdateProfile + audit)
- [x] **18 tests total — todos pasando**

### 🔄 En Progreso / Pendiente

#### Backend
- [ ] EmailService (MailKit + Quartz.NET)
- [ ] Health Checks endpoints (/health, /health/ready)
- [ ] Avatar upload / webcam
- [ ] Confirmación de email
- [ ] Export/import de usuarios (CSV)
- [ ] Filtros globales (AuditFilter, PerformanceFilter)
- [ ] Refresh token reuse detection (por ahora solo rotación)

#### Frontend
- [ ] Dark/light mode toggle
- [ ] Webcam para foto de perfil
- [ ] Avatar upload
- [ ] Confirmación de email
- [ ] Sistema de toasts (sonner)
- [ ] Internacionalización (i18n)
- [ ] Versión responsive completa

#### Infraestructura
- [ ] GitHub Actions CI/CD
- [ ] Despliegue con Traefik
- [ ] Backups automáticos
- [ ] Prometheus / Grafana (opcional)

### 🐛 Issues Conocidos

1. **FluentValidation auto-validation removido**: El paquete `FluentValidation.AspNetCore` v11 era incompatible con FluentValidation v12. Los validadores siguen registrados en DI para uso manual o vía MediatR pipeline. La validación de modelos funciona via `[ApiController]` + data annotations implícitas.
2. **Nullable warnings CS8625**: ~10 warnings por pasar `null` a `string?` en parámetros de audit logging. Cosméticos, seguros.
3. **EF Core query filter**: Se agregaron `HasQueryFilter` a RefreshToken y UserRole para coincidir con el filtro global de soft-delete en User.

### 📐 Stack Tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| Backend | .NET | 10.0 |
| Frontend | React + Vite + TypeScript | 19.x |
| UI | Tailwind CSS + shadcn/ui | 4.x |
| BD | PostgreSQL | 16+ |
| ORM | Entity Framework Core | 10.x |
| Auth | JWT (HS512, Access 15min + Refresh 7d rotation) | — |
| Logs | Serilog | — |
| Validación | FluentValidation | 12.x |
| Testing | xUnit + Moq + FluentAssertions | — |

### 📁 Estructura Actual

```
netReactMVP/
├── planInicial.ia.md                    # Plan original completo
├── planInicial.md                       # Este archivo (estado actual)
├── UserManagement.slnx
├── src/
│   └── backend/
│       ├── UserManagement.Domain/
│       │   └── Entities/                # User, Role, Permission, RolePermission, RefreshToken, AuditLog, LoginAttempt
│       ├── UserManagement.Application/
│       │   ├── Common/
│       │   │   ├── Interfaces/          # IUnitOfWork, repositorios, servicios
│       │   │   └── Validators/          # FluentValidation validators + DTOs
│       ├── UserManagement.Infrastructure/
│       │   ├── Persistence/
│       │   │   ├── Configurations/      # EF Core entity configs
│       │   │   └── Repositories/        # GenericRepository + repositorios
│       │   └── Services/                # AuditService, PasswordPolicyService
│       └── UserManagement.WebApi/
│           ├── Controllers/             # Auth, Users, Roles, Permissions, Profile, Admin
│           ├── Middleware/              # SecurityHeadersMiddleware
│           ├── Filters/                 # ApiResponse<T>
│           └── Program.cs
├── src/frontend/
│   └── src/
│       ├── components/
│       │   ├── auth/                    # SessionWarning
│       │   ├── layout/                  # Layout, Sidebar (colapsable), Header
│       │   └── ui/                      # shadcn/ui components
│       ├── pages/                       # login, dashboard, users, roles, permissions, profile, admin
│       ├── stores/                      # auth-store (Zustand)
│       └── lib/                         # api (axios instance), utils
├── tests/
│   ├── UserManagement.Application.Tests/ (14 tests)
│   └── UserManagement.WebApi.Tests/     (4 tests)
└── docker/                              # Dockerfiles, nginx.conf, docker-compose.yml
```

### 🔑 Endpoints Implementados

```
Autenticación:
  POST /api/auth/login              ✅ (lockout, audit, rate limit)
  POST /api/auth/refresh            ✅ (rotación)
  POST /api/auth/logout             ✅
  POST /api/auth/change-password    ✅ (verificación clave actual + política)
  POST /api/auth/forgot-password    ✅ (genera clave temporal)

Usuarios:
  GET    /api/users                 ✅ (paginado, búsqueda)
  GET    /api/users/{id}            ✅
  POST   /api/users                 ✅
  PUT    /api/users/{id}            ✅
  DELETE /api/users/{id}            ✅ (soft delete)
  PATCH  /api/users/{id}/activate   ✅
  PATCH  /api/users/{id}/reset-password ✅

Perfil:
  GET    /api/profile               ✅
  PUT    /api/profile               ✅
  GET    /api/profile/activity      ✅

Roles:
  GET    /api/roles                 ✅
  GET    /api/roles/{id}            ✅ (con permisos)
  POST   /api/roles                 ✅
  PUT    /api/roles/{id}            ✅
  DELETE /api/roles/{id}            ✅ (protegido sistema)
  PATCH  /api/roles/{id}/permissions ✅

Permisos:
  GET    /api/permissions           ✅
  GET    /api/permissions/modules   ✅

Admin:
  GET    /api/admin/dashboard       ✅ (métricas)
  GET    /api/admin/audit-log       ✅ (paginado)
  POST   /api/admin/revoke-all-tokens ✅
```

### 👤 Credenciales Seed

| Email | Contraseña | Rol |
|-------|-----------|-----|
| admin | admin | SuperAdmin (exige cambio de clave al primer ingreso) |

### 🚀 Para Ejecutar

```bash
# Terminal 1 — Backend
docker run -d --name mvp-postgres -e POSTGRES_DB=mvp-usuarios-db -e POSTGRES_USER=mvp-usuarios-db -e POSTGRES_PASSWORD=mvp-usuarios-dev-2024 -p 5432:5432 postgres:18-alpine
dotnet run --project src/backend/UserManagement.WebApi --launch-profile http

# Terminal 2 — Frontend
cd src/frontend && npm run dev

# Tests
dotnet test UserManagement.slnx
```
