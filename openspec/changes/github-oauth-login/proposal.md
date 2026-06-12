## Why

Agregar login con GitHub OAuth2 como segundo proveedor de autenticación externa, siguiendo el mismo patrón ya implementado con Google OAuth. Esto permite a usuarios registrarse automáticamente con su cuenta de GitHub (cuenta personal gratuita), obtener el rol `public` y acceder a la página pública de la plataforma. No reemplaza ningún método existente — se agrega como alternativa.

## What Changes

- Nuevo backend: `GitHubAuthController` (`GET /api/auth/github/login` y `GET /api/auth/github/callback`)
- Nuevo servicio: `GitHubAuthService` (implementa `IGitHubAuthService`) con flujo OAuth2 de GitHub (sin OpenID Connect)
- Nuevo CQRS: `GitHubLoginCommand` + `GitHubLoginCommandHandler` (misma lógica que Google: exchange code → buscar/crear usuario → asignar rol public → generar tokens)
- Nuevos modelos: `GitHubOptions`, `GitHubUserInfo`
- Nuevo rate limiting: `GitHub` policy (10 req/min)
- Frontend: botón "Continuar con GitHub" en login.tsx (reusa el mismo `OAuthCallbackPage`)
- Configuración: `Authentication:GitHub` en appsettings.json, .env.template, docker-compose.yml
- README.md: sección "GitHub OAuth 2.0 — Configuración" con paso a paso para crear OAuth App en GitHub
- No se crean nuevos roles/permisos — se reusa el rol `public` y permiso `page-public:view` existentes

## Capabilities

### New Capabilities
- `github-oauth`: Autenticación OAuth2 con GitHub — registro automático, asignación de rol public, generación de JWT + refresh tokens

### Modified Capabilities
- *(ninguno — es una nueva capacidad, no modifica requisitos de capacidades existentes)*

## Impact

- **Backend**: Nuevos archivos en `Application/Features/Auth/Commands/GitHubLogin/`, `Application/Common/Models/GitHub*`, `Infrastructure/Services/GitHubAuthService.cs`, `WebApi/Controllers/GitHubAuthController.cs`
- **Frontend**: Un botón adicional en `login.tsx`
- **Configuración**: `appsettings.json` (nueva sección `Authentication:GitHub`), `.env.template`, `docker-compose.yml`
- **Documentación**: `README.md` — nueva sección de configuración de GitHub OAuth
- **Tests**: Nuevos tests para handler, controller, validator, entity (mirando estructura de Google)
