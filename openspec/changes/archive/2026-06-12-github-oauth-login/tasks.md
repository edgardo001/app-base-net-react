## 1. Backend — GitHub OAuth Service & Models

- [x] 1.1 Create `Application/Common/Models/GitHubOptions.cs` — record with `ClientId`, `ClientSecret`, `RedirectUri` (same pattern as `GoogleOptions`)
- [x] 1.2 Create `Application/Common/Models/GitHubUserInfo.cs` — record with `ProviderId`, `Email`, `FirstName`, `LastName` (same pattern as `GoogleUserInfo`)
- [x] 1.3 Add `IGitHubAuthService` interface to `Application/Common/Interfaces/IServices.cs` with `GetAuthorizationUrl(string state)` and `ExchangeCodeAsync(string code, string state, CancellationToken)`
- [x] 1.4 Create `Infrastructure/Services/GitHubAuthService.cs` — implements `IGitHubAuthService`:
  - `GetAuthorizationUrl`: build URL with `client_id`, `redirect_uri`, `state`, `scope=read:user%20user:email`
  - State store via `ConcurrentDictionary<string, string>` (in-memory, same as Google)
  - `ExchangeCodeAsync`: POST to `https://github.com/login/oauth/access_token` → GET `https://api.github.com/user` + GET `https://api.github.com/user/emails` → map to `GitHubUserInfo` with fallbacks (name → firstName/lastName split, email fallback `{login}@github.local`)

## 2. Backend — GitHub OAuth CQRS

- [x] 2.1 Create `GitHubLoginCommand.cs` — record with `Code`, `State`, `IpAddress`, `UserAgent`, `FrontendUrl` (same shape as `GoogleLoginCommand`)
- [x] 2.2 Create `GitHubLoginOutcome.cs` — `GitHubLoginResult` + `GitHubLoginErrorCode` enum (None, AuthFailed, InvalidState), same pattern as `GoogleLoginOutcome`
- [x] 2.3 Create `GitHubLoginCommandValidator.cs` — FluentValidation: require non-empty `Code` and `State`
- [x] 2.4 Create `GitHubLoginCommandHandler.cs` — same logic as `GoogleLoginCommandHandler` but using `IGitHubAuthService`:
  1. Call `_githubAuth.ExchangeCodeAsync(code, state)`
  2. Check `_uow.ExternalLogins.GetByProviderAsync("github", providerId)`
  3. If linked → use linked user
  4. If not linked but email exists → create ExternalLogin link
  5. If completely new → create User (no password, email confirmed, RegistrationSource = "github")
  6. Assign `public` role if not already assigned
  7. `MarkLogin()`, generate JWT + refresh tokens, return `GitHubLoginOutcome`

## 3. Backend — GitHub Auth Controller

- [x] 3.1 Create `WebApi/Controllers/GitHubAuthController.cs`:
  - Route: `api/auth/github`
  - `[EnableRateLimiting("GitHub")]`
  - `GET login` → generate 32-byte state → `GetAuthorizationUrl(state)` → redirect
  - `GET callback` → validate code/state → `GitHubLoginCommand` → `mediator.Send()` → redirect with tokens on success, error redirect on failure
  - Same `_frontendUrl` logic as GoogleAuthController

## 4. Backend — Configuration & DI

- [x] 4.1 Add `Authentication:GitHub` section to `appsettings.json` with `ClientId`, `ClientSecret`, `RedirectUri`
- [x] 4.2 Register `GitHubOptions` in `DependencyInjection.cs`: `services.Configure<GitHubOptions>(configuration.GetSection("Authentication:GitHub"))`
- [x] 4.3 Register `IGitHubAuthService` HttpClient in `DependencyInjection.cs`: `services.AddHttpClient<IGitHubAuthService, GitHubAuthService>(...)`
- [x] 4.4 Add "GitHub" rate limiting policy to `Program.cs` (10 req/min, same as Google)
- [x] 4.5 Add `"GitHub"` section to `RateLimiting` in `appsettings.json`

## 5. Frontend — Login Button

- [x] 5.1 Add "Continuar con GitHub" button to `login.tsx` (after the Google button, same pattern: SVG GitHub logo + `window.location.href = '/api/auth/github/login'`)

## 6. Environment & Docker

- [x] 6.1 Add `Authentication__GitHub__*` variables to `.env.template`
- [x] 6.2 Add `Authentication__GitHub__*` environment variables to `docker-compose.yml` backend service

## 7. README Documentation

- [x] 7.1 Add "GitHub OAuth 2.0 — Configuración" section to `README.md` with step-by-step:
  - Cómo crear una OAuth App en GitHub (Settings → Developer settings → OAuth Apps)
  - Cómo obtener Client ID y Client Secret
  - Cómo configurar Authorization callback URL
  - Cómo configurar variables de entorno
  - Notas importantes (diferencia con Google: email privado, sin OpenID Connect)

## 8. Tests — Backend

- [x] 8.1 Create `GitHubAuthControllerTests.cs` in `WebApi.Tests/Controllers/` — test Login redirect, Callback with valid/invalid code/state, failed outcomes (mirror `GoogleAuthControllerTests`)
- [x] 8.2 Create `GitHubLoginCommandHandlerTests.cs` in `Application.Tests/Features/Auth/Commands/GitHubLogin/` — test new user, existing user by email, existing linked user, invalid state, auth failed (mirror `GoogleLoginCommandHandlerTests`)
- [x] 8.3 Create `GitHubLoginCommandValidatorTests.cs` in `Application.Tests/Features/Auth/Commands/GitHubLogin/` — test valid/invalid commands (mirror `GoogleLoginCommandValidatorTests`)
- [x] 8.4 Create `GitHubAuthServiceTests.cs` in `Application.Tests/Services/` — test `GetAuthorizationUrl` returns correct URL, test `ExchangeCodeAsync` with various GitHub API responses (success, email fallback, name mapping)

## 9. Verification

- [x] 9.1 Run `dotnet build AppBaseNetReact.slnx` — verify no build errors
- [x] 9.2 Run `dotnet test AppBaseNetReact.slnx` — verify all tests pass (including existing + new)
- [x] 9.3 Run frontend `npm run build` — verify no TypeScript/build errors
