## Why

The `AuthController` still orchestrates three password-management endpoints directly (`ChangePassword`, `ForgotPassword`, `ResetPassword`). With `cqrs-auth-login` and `cqrs-auth-refresh` already in production (Login + Refresh + Logout migrated to MediatR), these three are the last public/authenticated flows in `AuthController.cs` that violate the target architecture defined in `AGENTS.md` (see the "¿Dónde ocurre la acción?" table). The remaining `ConfirmEmail` flow will be migrated as a follow-up.

## What Changes

- Move the business orchestration of `ChangePassword` (authenticated) from `AuthController` to a new `ChangePasswordCommandHandler` in `Application/Features/Auth/Commands/ChangePassword/`.
- Move the business orchestration of `ForgotPassword` (public, anti-enumeration) to a new `ForgotPasswordCommandHandler` in `Application/Features/Auth/Commands/ForgotPassword/`.
- Move the business orchestration of `ResetPassword` (public, token-based) to a new `ResetPasswordCommandHandler` in `Application/Features/Auth/Commands/ResetPassword/`.
- Add a `PasswordResult` value object + 3 typed outcomes (`ChangePasswordOutcome`, `ForgotPasswordOutcome`, `ResetPasswordOutcome`) and a shared `PasswordErrorCode` enum.
- Add 3 new `INotification` records: `PasswordChangedNotification`, `PasswordResetRequestedNotification`, `PasswordResetNotification` + 3 audit handlers in `Infrastructure/Notifications/`.
- Add a `SendPasswordChangedEmailHandler` in `Infrastructure/Notifications/` that uses `IEmailService.SendPasswordChangedEmailAsync` (new method on the abstraction, mirroring the `SendAccountLockedEmailAsync` pattern from `cqrs-auth-login`).
- Refactor `AuthController.ChangePassword`, `AuthController.ForgotPassword`, `AuthController.ResetPassword` to thin MediatR senders.
- Remove the `Serilog` `using` (no longer needed once `Log.Warning` on email failure moves into the notification handler).
- Preserve the HTTP contract bit-for-bit: same status codes (200/400/401/404), same response shapes, same anti-enumeration behavior on `ForgotPassword`, same `User.FindFirst("sub")` reading on `ChangePassword`.
- Add 3 new capabilities to `openspec/specs/`: `auth-change-password`, `auth-forgot-password`, `auth-reset-password`.
- Add tests: 6 controller tests (Regla de Oro, against the existing controller before refactor) + 12 handler tests (3 scenarios × 3 commands + 3 happy paths).

## Capabilities

### New Capabilities
- `auth-change-password`: authenticated change-password flow for the currently signed-in user. Revokes all refresh tokens, audits `PasswordChanged`, sends confirmation email.
- `auth-forgot-password`: public anti-enumeration flow that issues a password-reset email when the email is registered; returns the same opaque success message otherwise.
- `auth-reset-password`: public token-validated flow that consumes a reset token, sets a new password, forces password change on next login, and confirms the email as a side effect.

### Modified Capabilities
(none — `auth-login`, `auth-refresh`, `auth-logout` are unchanged.)

## Impact

- **Code**: 3 new commands, 3 handlers, 3 outcome types, 3 notifications, 4 notification handlers (3 audit + 1 email), 1 new method on `IEmailService`, ~15 new tests. Controller loses ~120 LOC of business logic.
- **APIs**: zero breaking changes — endpoints keep the same paths, methods, and JSON shapes.
- **Dependencies**: none added; reuse existing `IUnitOfWork`, `IJwtService`, `IPasswordHasherService`, `IPasswordPolicyService`, `IAuditService`, `IEmailService`, `IDateTimeProvider`.
- **DB**: zero migration; all fields (`EmailConfirmationToken`, `EmailConfirmationTokenExpires`) already exist on the `User` entity and are reused for password reset (current controller behavior).
- **Frontend**: zero changes — no DTO shapes change.
