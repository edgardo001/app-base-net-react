# AppBaseNetReact.WebApi — Punto de Entrada (Presentación)

## Propósito

Capa de presentación que expone la API REST. Es el **punto de entrada** de la aplicación. Orquesta las peticiones HTTP y delega en handlers CQRS via `IMediator.Send()`.

## Dependencias

- **Referencia:** `Application` + `Infrastructure` (pragmatic hexagonal — no referencia `Domain` directamente)
- **Paquetes NuGet:** ASP.NET Core, Rate Limiting, OpenAPI/Scalar, Serilog

## Estructura

```
Controllers/
  AuthController.cs         — Login, Refresh, Logout, ChangePassword, ForgotPassword, ResetPassword
  UsersController.cs        — CRUD usuarios + Activar/Desactivar, ResetPassword, RevokeTokens
  RolesController.cs        — CRUD roles + actualizar permisos
  PermissionsController.cs  — Listar permisos agrupados por módulo
  ProfileController.cs      — Ver/actualizar perfil, ver actividad reciente
  AdminController.cs        — Dashboard stats, auditoría global, revocar todos los tokens
Middleware/
  ExceptionHandlingMiddleware.cs  — Captura global de excepciones → ApiResponse<T>
  SecurityHeadersMiddleware.cs    — CSP, X-Frame-Options, X-Content-Type-Options, etc.
Filters/
  ApiResponse.cs             — ApiResponse<T>, PagedResponse<T>
Properties/
  launchSettings.json        — Perfiles de lanzamiento (http, https, Docker)
Extensions/                  — Extensiones de configuración (reservado)
Program.cs                   — Punto de entrada: builder + pipeline de middleware
appsettings.json             — Configuración (JWT, ConnectionStrings, PasswordPolicy, etc.)
```

## Pipeline de Middleware (orden en Program.cs)

```
ExceptionHandling → SecurityHeaders → RateLimiting → CORS → Auth → Authorization → Controllers
```

## ✅ Estado actual

Todos los controladores delegan en **handlers CQRS** en `Application/Features/` via `MediatR.Send()`. Ningún controller inyecta `IUnitOfWork` directamente.
