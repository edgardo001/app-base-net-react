## Why

The architecture diagram in `AGENTS.md` defines a target state where feature flows run through MediatR Command/Query → Handler → Validator → DTO/Response, with `ValidationBehavior` in the MediatR pipeline. Today, every controller — including `AuthController` — orchestrates the use case directly: it injects `IUnitOfWork`, `IJwtService`, `IPasswordHasherService`, `IPasswordPolicyService`, `IAuditService`, `IEmailService`, `EmailRenderer`, `EmailOptions` and `IConfiguration`, and runs the entire login workflow inline. The `Application/Features/*/Commands/` and `Application/Features/*/Queries/` folders are scaffolded but empty; MediatR is registered but unused; the only existing handler, `ValidationBehavior`, has nothing to validate.

The `Login` flow is the most security-sensitive and most-trafficked endpoint, and it is the only Auth flow that touches the full set of cross-cutting concerns (password verification, lockout policy, email confirmation, account state checks, JWT issuance, refresh token rotation, audit logging, account-locked email). It is the right feature to migrate first: if the pattern works for Login, it works for everything else. Migrating Login end-to-end establishes the conventions the rest of the auth flows (Refresh, Logout, ChangePassword, ForgotPassword, ResetPassword, ConfirmEmail) will follow in subsequent changes.

This change does **not** add behavior. The goal is to preserve the exact same API contract (`POST /api/auth/login` response shape, status codes, side effects) while moving orchestration out of the controller and into a thin Application-layer handler.

## What Changes

- **Add** `Application/Features/Auth/Commands/Login/` with `LoginCommand` (record implementing `IRequest<LoginOutcome>`), `LoginOutcome` (result wrapper with `LoginResult` + optional `LoginResponse`), `LoginResponse` (DTO), `LoginCommandValidator` (`AbstractValidator<LoginCommand>`).
- **Add** `LoginCommandHandler` in the same folder that:
  - Loads the user, verifies the password, increments the failed-access counter, applies lockout, persists `LoginAttempt`, builds and persists the `RefreshToken`, publishes domain notifications, and returns the `LoginOutcome` (or a typed failure).
  - Does **not** know about `HttpContext`, `EmailRenderer`, `EmailOptions`, or `IConfiguration`.
- **Add** `Application/Features/Auth/Notifications/AuthNotifications.cs` with three `INotification` records: `UserLoggedInNotification`, `UserLoginFailedNotification`, `AccountLockedNotification`.
- **Add** `Infrastructure/Notifications/` with three MediatR notification handlers:
  - `UserLoggedInAuditHandler` — writes the `UserLoggedIn` audit log entry.
  - `UserLoginFailedAuditHandler` — writes the `UserLoginFailed` audit log entry.
  - `AccountLockedEmailHandler` — calls `IEmailService.SendAccountLockedEmailAsync` to send the lockout email.
- **Extend** `IEmailService` with `SendAccountLockedEmailAsync(to, userName, lockoutMinutes, resetLink, ct)` so the Application handler can request the email without depending on `EmailRenderer`/`EmailOptions`. Implementation lives in `Infrastructure/Email/EmailService.cs` and reuses the existing `AccountLocked` template.
- **Refactor** `AuthController.Login` to a thin MediatR sender: it builds the `LoginCommand` (passing `IpAddress`, `UserAgent`, and `FrontendUrl` resolved from `IConfiguration`), calls `IMediator.Send`, and maps the `LoginOutcome` to the existing `ApiResponse<T>` shape and HTTP status codes (200 / 401 / 403 / 423). No business logic remains in the controller.
- **Add** `LoginCommandHandlerTests` in `AppBaseNetReact.Application.Tests/Features/Auth/Commands/Login/` covering all 8 scenarios currently asserted in `AuthControllerTests` for the Login endpoint (valid creds, invalid password, unknown email, deactivated, locked, unconfirmed email, lockout threshold + email sent, password-expired flag).
- **Update** `AGENTS.md` and the architecture diagram to mark the Login flow as migrated (⚡ → 🎯 for that feature only). README is not affected (no API changes).
- **Tests**:
  - Add the 8 new login scenarios to `AuthControllerTests` (proves the controller's pre-refactor behavior) **before** the refactor — Regla de Oro.
  - Add `LoginCommandHandlerTests` with equivalent coverage of the handler in isolation.
  - All 32 existing tests must remain green throughout.

## Capabilities

### New Capabilities
- `auth-login`: Vertical slice covering the `POST /api/auth/login` endpoint through MediatR (command, handler, validator, response, notification handlers) and the preserved API contract (status codes, response shape, side effects: lockout, email, audit, refresh token).

### Modified Capabilities
(none — no existing capability spec exists yet; this change introduces the first one.)

## Impact

- **Backend**:
  - New files in `Application/Features/Auth/Commands/Login/` (5 files) and `Application/Features/Auth/Notifications/` (1 file).
  - New files in `Infrastructure/Notifications/` (3 files).
  - Modified: `AuthController.cs` (Login method shrinks to ~10 lines), `IEmailService` interface (1 new method), `EmailService` implementation (1 new method), `AppBaseNetReact.WebApi.Tests/Controllers/AuthControllerTests.cs` (8 new tests), `AppBaseNetReact.Application.Tests/Features/Auth/Commands/Login/LoginCommandHandlerTests.cs` (new).
  - DI registrations added: 3 notification handlers (auto-registered by `AddMediatR` once the assembly contains them — no manual registration needed if `RegisterServicesFromAssembly` is already called, which it is in `DependencyInjection.cs`).
  - **No breaking change** to the HTTP contract. The Login endpoint returns the same JSON shape and status codes.
  - **No DB migration** required. No entity changes. `LoginAttempt`, `RefreshToken`, `User`, `AuditLog` schemas are unchanged.
- **Frontend**: none.
- **Config**: none (reuses `Email.Templates.AccountLocked` which is already defined in `appsettings.json`).
- **Build/test**: 32 → 40 baseline tests (8 new) before refactor; +8 handler tests after refactor → 48 total.
