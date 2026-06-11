## 1. Domain Layer — Entidades y Modelos

- [x] 1.1 Crear `ExternalLogin.cs` en Domain/Entities con propiedades: `Id`, `UserId`, `Provider` (string, ej: "google"), `ProviderId` (string, ej: Google `sub` claim), `ProviderEmail` (string), `CreatedAt`. Relación N:1 con `User`
- [x] 1.2 Actualizar `User.cs`: agregar `ICollection<ExternalLogin> ExternalLogins` navigation property. Cambiar `PasswordHash` de `string` a `string?` (nullable) para soportar usuarios passwordless
- [x] 1.3 Crear `ExternalLoginConfiguration` en Configurations: tabla `ExternalLogins`, unique index en `(Provider, ProviderId)`, FK a `Users` con cascade delete

## 2. Application Layer — Puertos y Casos de Uso

- [x] 2.1 Agregar `IExternalLoginRepository : IRepository<ExternalLogin>` con método `Task<ExternalLogin?> GetByProviderAsync(string provider, string providerId, CancellationToken ct)`
- [x] 2.2 Agregar `IExternalLoginRepository ExternalLogins` a `IUnitOfWork`
- [x] 2.3 Crear clase `GoogleOptions` en Common/Models (o Settings) con `ClientId`, `ClientSecret`, `RedirectUri` bindeable desde `Authentication__Google__*`
- [x] 2.4 Crear interface `IGoogleAuthService : ITransientService` en Common/Interfaces con métodos:
      - `string GetAuthorizationUrl(string state)` (genera URL de Google OAuth)
      - `Task<GoogleUserInfo> ExchangeCodeAsync(string code, string state, CancellationToken ct)` (valida state, intercambia code, verifica ID token, retorna info del usuario)
- [x] 2.5 Crear `GoogleUserInfo` record en Common/Models con: `ProviderId`, `Email`, `FirstName`, `LastName`
- [x] 2.6 Crear `GoogleLoginCommand` record en Features/Auth/Commands/GoogleLogin/: `GoogleLoginCommand(string Code, string State, string? IpAddress, string? UserAgent, string FrontendUrl)` implementando `IRequest<GoogleLoginOutcome>`
- [x] 2.7 Crear `GoogleLoginResult` + `GoogleLoginOutcome` records (mismo patrón que `LoginResult`/`LoginOutcome` pero sin password validation, solo `Success()` o `Fail()`)
- [x] 2.8 Crear `GoogleLoginCommandHandler` que:
      1. Llama a `IGoogleAuthService.ExchangeCodeAsync(code, state)`
      2. Busca `ExternalLogin` por Provider="google" + ProviderId
      3. Si no existe, busca `User` por email (vinculación automática) o crea nuevo User con `PasswordHash=null`, `EmailConfirmed=true`, `IsActive=true`
      4. Si el usuario es nuevo, asigna rol `public`
      5. Genera JWT + refresh token (mismo patrón que LoginCommandHandler)
      6. Redirige al frontend con `/oauth-callback#accessToken=...`
- [x] 2.9 Crear `GoogleLoginCommandValidator` con FluentValidation (Code requerido, State requerido)
- [x] 2.10 Actualizar `LoginCommandHandler`: al validar password, si `user.PasswordHash == null` retornar "Invalid email or password" (mismo mensaje anti-enumeration)

## 3. Infrastructure Layer — Adaptadores

- [x] 3.1 Crear `ExternalLoginRepository` en Persistence/Repositories implementando `IExternalLoginRepository`
- [x] 3.2 Actualizar `UnitOfWork`: agregar `ExternalLoginRepository` y propiedad `IExternalLoginRepository ExternalLogins`
- [x] 3.3 Crear `GoogleAuthService` en Infrastructure/Services que implementa `IGoogleAuthService`:
      - Usa `HttpClient` para llamar a Google OAuth endpoints
      - Implementa `GetAuthorizationUrl()` generando URL con `response_type=code`, `client_id`, `redirect_uri`, `scope=openid email profile`, `state=nonce`
      - Almacena `state` nonce en memoria (cache con expiración de 10 min) para validación anti-CSRF
      - Implementa `ExchangeCodeAsync()`: llama a `POST https://oauth2.googleapis.com/token`, verifica ID token con `GoogleJsonWebSignature.ValidateAsync()` (del paquete `Google.Apis.Auth`), extrae claims
- [x] 3.4 Agregar `Google.Apis.Auth` NuGet package al proyecto Infrastructure
- [x] 3.5 Registrar `IGoogleAuthService` en `Infrastructure/DependencyInjection.cs` como `AddScoped` + registrar `HttpClient` para GoogleAuthService
- [x] 3.6 Actualizar `DatabaseSeeder.cs`: agregar permiso `Permission.Create("page-public:view", "Ver página pública", "Público", "Permite acceder a la página pública de bienvenida")` y rol `Role.Create("public", "Rol público para usuarios que ingresan vía OAuth", isSystem: true)` con ese permiso asignado
- [x] 3.7 Agregar configuración de `GoogleOptions` en DependencyInjection (bind desde `Authentication__Google__*`)
- [x] 3.8 Agregar `builder.Property(u => u.PasswordHash).IsRequired(false)` en `UserConfiguration` (nullable)
- [x] 3.9 Crear migración EF Core para ExternalLogin + PasswordHash nullable

## 4. WebApi Layer — Endpoints

- [x] 4.1 Crear `GoogleAuthController` en Controllers/ con:
      - `GET /api/auth/google/login` — inicia flujo: genera state nonce, retorna 302 redirect a Google OAuth URL
      - `GET /api/auth/google/callback` — recibe code + state de Google, delega en `GoogleLoginCommandHandler`, retorna redirect a frontend con tokens en hash fragment
- [x] 4.2 Agregar rate limiting policy `Google` (10/min) en Program.cs y aplicar `[EnableRateLimiting("Google")]` al controller
- [x] 4.3 Excluir `GET /api/auth/google/callback` de CSRF Middleware (es una redirección GET, no lleva CSRF token). Agregar la ruta al listado de exclusión en CsrfMiddleware
- [x] 4.4 Agregar configuración `Authentication__Google__*` en `appsettings.json` / `appsettings.Development.json`

## 5. Frontend Layer — UI

- [x] 5.1 Crear página `OAuthCallbackPage` en `pages/oauth-callback.tsx`:
      - Lee `window.location.hash` para extraer `accessToken`, `refreshToken`, `expiresAt`
      - Almacena tokens en localStorage
      - Actualiza Zustand auth store
      - Redirige a `/publico`
      - Limpia el hash de la URL (`window.location.hash = ''`)
      - Si hay error en query params (`?error=access_denied`), redirige a `/login?error=google_auth_failed`
      - Lee `window.location.hash` para extraer `accessToken`, `refreshToken`, `expiresAt`
      - Almacena tokens en localStorage
      - Actualiza Zustand auth store
      - Redirige a `/publico`
      - Limpia el hash de la URL (`window.location.hash = ''`)
      - Si hay error en query params (`?error=access_denied`), redirige a `/login?error=google_auth_failed`
- [x] 5.2 Crear página `PublicoPage` en `pages/publico.tsx`:
      - Muestra tarjeta centrada con el mensaje: "Hola {nombre}, gracias por registrarte en mi plataforma, no haremos nada raro con tus datos, ya que esta es solo una app de aprendizaje, tal vez en algún futuro verás algo muy interesante en este lugar, pero de momento solo tienes acceso a esta página."
      - Muestra el nombre del usuario desde el store
      - Incluye sidebar/header (usa el layout existente)
- [x] 5.3 Actualizar `App.tsx`:
      - Agregar ruta `/oauth-callback` (pública, fuera de ProtectedRoute)
      - Agregar ruta `/publico` dentro de ProtectedRoute + `AuthorizedRoute` con `requiredPermission="page-public:view"` (dentro del layout)
- [x] 5.4 Actualizar `login.tsx`: agregar botón "Sign in with Google" debajo del formulario de login (separador visual con línea "o"), que redirige a `/api/auth/google/login`. Estilo: botón con ícono de Google y texto "Continuar con Google"

## 6. Configuración y Documentación

- [x] 6.1 Agregar variables de entorno al `.env.template`:
      - `Authentication__Google__ClientId`
      - `Authentication__Google__ClientSecret`
      - `Authentication__Google__RedirectUri`
- [x] 6.2 Agregar sección `Authentication:Google` al `appsettings.json` con valores placeholder
- [x] 6.3 Actualizar `README.md` con:
      - Instrucciones paso a paso para configurar Google Cloud Console (crear proyecto, habilitar Google+ API, crear credenciales OAuth 2.0 Web Application, configurar URIs de redirección)
      - Nota: cuenta gratuita personal es suficiente
      - Lista de variables de entorno requeridas

## 7. Testing

- [x] 7.1 Crear `ExternalLoginEntityTests` en proyecto de tests: verificar creación, unique constraint (Provider+ProviderId), relación con User
- [x] 7.2 Crear `GoogleLoginCommandHandlerTests`:
      - Test: new user via Google → se crea User + ExternalLogin + se asigna rol public
      - Test: existing user via Google (mismo email) → se vincula ExternalLogin, no se asigna rol public
      - Test: existing linked user → login exitoso
      - Test: invalid Google code → retorna error
- [x] 7.3 Crear `GoogleAuthControllerTests` (controller tests con HttpContext mockeado):
      - Test: GET /api/auth/google/login → retorna 302 con Location a Google
      - Test: GET /api/auth/google/callback con code válido → redirige a frontend con tokens
- [x] 7.4 Actualizar tests existentes de `LoginCommandHandler`: verificar que passwordless user (PasswordHash=null) recibe "Invalid email or password"
