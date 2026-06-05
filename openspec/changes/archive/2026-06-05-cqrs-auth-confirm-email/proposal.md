## Why

`AuthController.ConfirmEmail` is the last remaining ⚡ flow in the auth domain. The controller still orchestrates user lookup, token expiry check, `user.ConfirmEmail()`, audit log, and welcome email inline. This breaks the CQRS pattern established by `cqrs-auth-login`, `cqrs-auth-refresh`, and `cqrs-auth-password`, and blocks further refactors of the controller (e.g. once this migrates, only `Program.cs` + `SendAccountLockedEmail` helper remain coupled to `IPasswordHasherService`/`EmailRenderer`).

## What Changes

- Migrate `AuthController.ConfirmEmail` to dispatch a `ConfirmEmailCommand` via MediatR.
- Add `ConfirmEmailCommand` + `ConfirmEmailCommandValidator` + `ConfirmEmailOutcome` + `ConfirmEmailCommandHandler` in `Application/Features/Auth/Commands/ConfirmEmail/`.
- Add `EmailConfirmationResult` + `EmailErrorCode` shared model in `Application/Common/Models/`.
- Add `EmailConfirmedNotification` in `Application/Features/Auth/Notifications/`.
- Add `EmailConfirmedAuditLogHandler` (writes audit) + `EmailConfirmedEmailHandler` (sends welcome email via new `IEmailService.SendWelcomeEmailAsync`).
- Extend `IEmailService` with `SendWelcomeEmailAsync`; implement in `EmailService` (parallels `SendAccountLockedEmailAsync` and `SendPasswordChangedEmailAsync`).
- Refactor `AuthController.ConfirmEmail` to read token, send `ConfirmEmailCommand`, map `EmailErrorCode` → HTTP (`None` → 200, `InvalidConfirmationToken`/`ConfirmationTokenExpired` → 400).
- Add 3 controller tests + 1 handler test + 1 email handler test; remove or update existing direct-UoW tests.
- Update `AGENTS.md` table to mark ConfirmEmail as ✅ Application-layer.

## Capabilities

### New Capabilities
- `auth-confirm-email`: User confirms their email address by presenting a single-use token; on success, the user is marked `EmailConfirmed`, an audit log is written, and a welcome email is sent.

### Modified Capabilities
- (none — no spec-level behavior changes; HTTP contract is preserved bit-for-bit)

## Impact

- **Code**:
  - `src/backend/AppBaseNetReact.Application/Common/Models/EmailConfirmationResult.cs` (new)
  - `src/backend/AppBaseNetReact.Application/Features/Auth/Commands/ConfirmEmail/` (new: 4 files)
  - `src/backend/AppBaseNetReact.Application/Features/Auth/Notifications/AuthNotifications.cs` (add 1 record)
  - `src/backend/AppBaseNetReact.Application/Common/Interfaces/IServices.cs` (add 1 method to `IEmailService`)
  - `src/backend/AppBaseNetReact.Infrastructure/Email/EmailService.cs` (implement `SendWelcomeEmailAsync`)
  - `src/backend/AppBaseNetReact.Infrastructure/Notifications/EmailConfirmedEmailHandler.cs` (new)
  - `src/backend/AppBaseNetReact.Infrastructure/Notifications/EmailConfirmedAuditLogHandler.cs` (new)
  - `src/backend/AppBaseNetReact.WebApi/Controllers/AuthController.cs` (refactor `ConfirmEmail`, remove `_renderer`/`_emailOptions` fields if no other in-controller use)
  - `src/backend/AppBaseNetReact.WebApi.Tests/Controllers/AuthControllerTests.cs` (replace 3 direct-UoW tests with 3 IMediator tests)
  - `src/backend/AppBaseNetReact.Application.Tests/Features/Auth/Commands/ConfirmEmail/ConfirmEmailCommandHandlerTests.cs` (new, 4-5 tests)
  - `src/backend/AppBaseNetReact.Application.Tests/Features/Auth/Notifications/EmailConfirmedEmailHandlerTests.cs` (new, 2 tests)
  - `AGENTS.md` (update migration table row for ConfirmEmail)
- **HTTP contract**: unchanged (status codes, response shapes preserved).
- **Database**: no schema changes.
- **Dependencies**: no new NuGet packages.
- **Configuration**: no env-var changes.
- **Docker**: no changes.
