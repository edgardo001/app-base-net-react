## Context

`cqrs-auth-login` and `cqrs-auth-refresh` migrated Login + Refresh + Logout to MediatR-based CQRS. This change extends the same pattern to the three password-management endpoints that still live in `AuthController`: `ChangePassword` (authenticated, requires JWT), `ForgotPassword` (public, anti-enumeration), and `ResetPassword` (public, token-based). `ConfirmEmail` is intentionally left for a follow-up because its primary purpose is email verification, not password management.

The current implementation has three pain points:

1. **Anti-enumeration is easy to break accidentally.** `ForgotPassword` returns the same opaque success message for both "user does not exist" and "reset email queued", but the path that emits the email is inlined. A future contributor adding a metric or a log line could break the parity and create a timing oracle.
2. **Password reset reuses the email-confirmation token field.** `User.EmailConfirmationToken` and `User.EmailConfirmationTokenExpires` are repurposed to carry the reset token. This is intentional (it avoids adding two new fields to the `User` table for a related flow), but the contract is not documented in code.
3. **`ChangePassword` reads the user id from the JWT in the controller.** A migration to a MediatR sender requires either a `IJwtClaimsAccessor` abstraction or a `Guid UserId` field on the command. The latter is the established pattern in this codebase (handlers are pure C# and free of `HttpContext`).

These flows also use a `SendEmail` helper that lives in `AuthController` and directly references `EmailRenderer` + `EmailOptions` — Infrastructure-layer types. After the migration, the email-sending side effect is fired through a MediatR `INotification`, and the email-renderer knowledge is contained in `Infrastructure/Notifications/SendPasswordChangedEmailHandler.cs`.

## Goals / Non-Goals

**Goals:**
- Move `AuthController.ChangePassword`, `AuthController.ForgotPassword`, and `AuthController.ResetPassword` orchestration into `ChangePasswordCommandHandler`, `ForgotPasswordCommandHandler`, and `ResetPasswordCommandHandler` in the Application layer.
- Preserve the HTTP contract bit-for-bit: same status codes (200/400/401/404), same response shapes, same anti-enumeration behavior, same `ForcePasswordChange` + `ConfirmEmail` side effects on `ResetPassword`.
- Reuse the existing `MediatR` / `INotification` infrastructure from `cqrs-auth-login` and `cqrs-auth-refresh`.
- Reuse the existing `IPasswordPolicyService` and `IPasswordHasherService` from the Application layer.
- Extract the "send password-changed email" concern behind `IEmailService.SendPasswordChangedEmailAsync` (mirrors the `SendAccountLockedEmailAsync` pattern from `cqrs-auth-login`).
- Remove the `Serilog` `using` from `AuthController.cs` (it was only used for the email-failure `Log.Warning`, which now lives in the notification handler with `ILogger<T>`).
- Keep the Application layer free of `HttpContext`, `EmailRenderer`, `EmailOptions`, and `IConfiguration`.

**Non-Goals:**
- Migrating `ConfirmEmail` — follow-up change.
- Adding 2FA, password-history, or breached-password checks.
- Splitting password reset into a dedicated `User.PasswordResetToken` column (current `EmailConfirmationToken` reuse is preserved).
- Adding a separate "expired reset token" email or grace period.
- Integration tests with Testcontainers.

## Decisions

| # | Decision | Choice | Alternatives considered | Rationale |
|---|----------|--------|-------------------------|-----------|
| 1 | **Result type for each command** | A shared `PasswordResult` + `PasswordErrorCode` enum (`None`, `InvalidCurrentPassword`, `WeakPassword`, `InvalidResetToken`, `ResetTokenExpired`, `UserNotFound`) returned via 3 typed outcomes (`ChangePasswordOutcome`, `ForgotPasswordOutcome`, `ResetPasswordOutcome`). ForgotPassword always returns success (anti-enumeration). | Separate result type per command; throw exceptions. | A shared enum mirrors the cross-cutting concern (password flows). A separate enum per command would create near-duplicate types. Anti-enumeration on ForgotPassword collapses to a single `Success` outcome regardless of whether the user existed. |
| 2 | **ChangePassword — how the handler learns the user id** | The command carries a `Guid UserId` field. The controller reads `User.FindFirst("sub")?.Value` and returns 401 immediately if the claim is missing/malformed — no handler call. | Inject `IHttpContextAccessor` into the handler; pass `ClaimsPrincipal` to the command. | The first keeps the Application layer free of HTTP/Claims. The second couples the command to `System.Security.Claims` and creates a leaky abstraction. The "user id from JWT" responsibility belongs to the controller (where ASP.NET Core lives). |
| 3 | **ForgotPassword anti-enumeration** | Single outcome: `ForgotPasswordOutcome(PasswordResult.Success())`. The handler branches internally: if the user is null, it does nothing; if the user exists, it generates a token, persists, audits, and publishes a notification. The controller always returns 200. | Different outcomes for "user exists" vs "user does not exist". | Anti-enumeration is the spec. A different outcome would create a side-channel. The audit + email are only fired when the user exists, but the HTTP response is identical. |
| 4 | **ResetPassword token field** | Continue reusing `User.EmailConfirmationToken` + `User.EmailConfirmationTokenExpires`. Document the dual-purpose in the command handler. | Add a new `User.PasswordResetToken` field with its own expiry. | The current behavior works. Adding a column is a DB migration that is explicitly out of scope. The field is named after email confirmation for historical reasons; the contract is "this token field is shared between the email-confirmation flow and the password-reset flow". |
| 5 | **`ResetPassword` side effects** | After token validation: `user.SetPasswordHash(...)`, `user.ForcePasswordChange()`, `user.ConfirmEmail()`, then a single `SaveChangesAsync`. Then publish `PasswordResetNotification` (which fans out to audit + email). | Save twice, publish before save. | Single save = atomic. The current code already saves once before publishing; the new code does the same. |
| 6 | **Email rendering** | The controller no longer touches `EmailRenderer` / `EmailOptions`. The new `SendPasswordChangedEmailHandler` (Infrastructure) calls `IEmailService.SendPasswordChangedEmailAsync`, which the existing `EmailService` is extended to implement (template lookup + `EmailRenderer.Render` stay in Infrastructure, never in Application). | Move `EmailRenderer` to Application. | `EmailRenderer` depends on file I/O and `EmailOptions` — pure Infrastructure concerns. The Application layer is now free of these references. |
| 7 | **3 new notifications** | `PasswordChangedNotification`, `PasswordResetRequestedNotification`, `PasswordResetNotification`. Each is fanned out to one audit handler + (for `PasswordChanged` and `PasswordReset`) one email handler. | Direct `IAuditService` + `IEmailService` calls from the command handler. | Consistent with `cqrs-auth-login` (audit) and `cqrs-auth-refresh` (audit). The 3rd notification for `PasswordResetRequested` is the audit signal for the "I clicked forgot" event; the email itself is a side effect of that notification. |
| 8 | **Test strategy** | Same 3-layer pattern as `cqrs-auth-refresh`: <br>• `AuthControllerTests` — 3 new ChangePassword tests + 3 ForgotPassword + 3 ResetPassword = 9 controller tests (Regla de Oro baseline). <br>• 3 new handler test files: `ChangePasswordCommandHandlerTests` (6), `ForgotPasswordCommandHandlerTests` (3), `ResetPasswordCommandHandlerTests` (4) = 13 handler tests. <br>• 1 new email-handler test: `SendPasswordChangedEmailHandlerTests` (2). | One mega-test per endpoint. | The Regla de Oro baseline must be added BEFORE the refactor (to catch any regression introduced by the refactor itself). The handler tests are the safety net for the new code. |
| 9 | **Persistence order** | Each handler does a single `SaveChangesAsync` at the end. ChangePassword: `SetPasswordHash` → `RevokeAllForUserAsync` → `SaveChangesAsync` → publish. ForgotPassword: `SetEmailConfirmationToken` → `SaveChangesAsync` → publish. ResetPassword: `SetPasswordHash` + `ForcePasswordChange` + `ConfirmEmail` → `SaveChangesAsync` → publish. | Multiple `SaveChangesAsync` calls. | Single save = atomic. The current controller code already saves once per endpoint; the new code does the same. |

```
┌──────────────────────────────────────────────────────────────────────────┐
│                  PASSWORD FLOWS — TARGET (CQRS)                           │
└──────────────────────────────────────────────────────────────────────────┘

POST /api/auth/change-password   POST /api/auth/forgot-password   POST /api/auth/reset-password
         │                                  │                                 │
         ▼                                  ▼                                 ▼
┌────────────────────────┐    ┌────────────────────────┐    ┌────────────────────────┐
│ AuthController         │    │ AuthController         │    │ AuthController         │
│ .ChangePassword        │    │ .ForgotPassword        │    │ .ResetPassword         │  (WebApi — thin)
│ • Reads "sub" claim    │    │ • Builds ForgotPassword│    │ • Builds ResetPassword │
│ • Returns 401 on miss  │    │   Command              │    │   Command              │
│ • Builds ChangePassword│    │ • Sends via IMediator  │    │ • Sends via IMediator  │
│   Command(UserId, …)   │    │ • Returns 200 always   │    │ • Maps outcome → 200/  │
│ • Maps outcome → 200/  │    └──────────┬─────────────┘    │   400                   │
│   400/401/404          │               │                  └──────────┬─────────────┘
└──────────┬─────────────┘               │                             │
           │ ChangePasswordCommand(      │ ForgotPasswordCommand(      │ ResetPasswordCommand(
           │   UserId, CurrentPassword,  │   Email, IpAddress, UA)     │   Token, NewPassword,
           │   NewPassword, Ip, UA)       ▼                             │   IpAddress, UA)
           ▼                  ┌──────────────────────────┐              ▼
┌────────────────────────┐    │ ForgotPasswordCommand    │   ┌────────────────────────┐
│ ChangePasswordCommand  │    │ Handler                  │   │ ResetPasswordCommand   │
│ Handler                │    │ • Lookup by email        │   │ Handler                │
│ • Verify current pwd   │    │ • If null → no-op (still │   │ • Lookup by token      │
│ • Validate new pwd     │    │   returns Success)       │   │ • If null/expired      │
│ • SetPasswordHash      │    │ • Else: SetEmailConfToken│   │   → Invalid/Expired    │
│ • RevokeAllForUser     │    │ • SaveChanges            │   │ • Validate new pwd     │
│ • SaveChanges          │    │ • Publish PasswordReset  │   │ • SetPasswordHash      │
│ • Publish PasswordChg  │    │   RequestedNotification  │   │ • ForcePasswordChange  │
│   Notification         │    └──────────┬───────────────┘   │ • ConfirmEmail         │
└──────────┬─────────────┘               │                   │ • SaveChanges          │
           │ _mediator.Publish           ▼                   │ • Publish PasswordReset│
           ▼                  ┌────────────────────────┐      │   Notification         │
┌────────────────────────┐    │ PasswordResetRequested │      └──────────┬─────────────┘
│ PasswordChangedAudit   │    │ AuditHandler           │                 │ _mediator.Publish
│ Handler                │    │ → IAuditService.LogAsync│                 ▼
│ → IAuditService.LogAsync│   │     "PasswordResetReq", │      ┌────────────────────────┐
│     "PasswordChanged", │    │     …                   │      │ PasswordResetAudit     │
│     …                  │    └────────────────────────┘      │ Handler                 │
└────────────────────────┘                                     │ + SendPasswordChanged  │
                                                              │   EmailHandler          │
                                                              └────────────────────────┘
```

## Risks / Trade-offs

- **[`EmailConfirmationToken` field is overloaded]** → The same DB column carries both the email-confirmation token (set during registration or by an admin) and the password-reset token (set during ForgotPassword). A user who triggers a password reset while having a pending email-confirmation token will silently overwrite the latter. Mitigation: the current code already has this behavior; the field is named for historical reasons. A future change can introduce a dedicated `PasswordResetToken` column. This is explicitly non-goal here. Acceptable.
- **[`ResetPassword` confirms the email as a side effect]** → A user who never received the welcome email (admin-created account) is now "confirmed" once they reset their password. Mitigation: this is the current behavior; preserving it. A future change could decouple these. Acceptable.
- **[Pattern spread: this is the 3rd auth migration]** → After this change, the conventions are effectively locked. The `PasswordResult` type intentionally reuses the `LoginResult` / `RefreshResult` shape so any future refactor affects all three uniformly. Mitigation: the `cqrs-auth-password` proposal calls out this is the last "shape-defining" change.
- **[9 new controller tests + 13 new handler tests = 22 new tests]** → Significant test-count growth. Mitigation: all are pure unit tests with `Moq`; total runtime stays under 1 second. The cost of NOT having them (regression in anti-enumeration, broken email-failure fallback) is much higher.
- **`SendEmail` helper removed from `AuthController` but still used by `ConfirmEmail`** → The `SendEmail` private method is still needed for the `ConfirmEmail` endpoint until that flow is migrated. After `cqrs-auth-confirm-email` (follow-up), the helper can be removed entirely. This change does not touch `ConfirmEmail`.

## Migration Plan

1. **Baseline** (Step 1 of `tasks.md`): add 9 controller tests to `AuthControllerTests.cs` against the *current* controller. Run `dotnet test` — confirm `72/72` green.
2. **Abstractions + handlers + tests** (Steps 2–5): create `PasswordResult`, `ChangePasswordCommand`/`Handler`/`Validator`/`Outcome`, `ForgotPasswordCommand`/`Handler`/`Validator`/`Outcome`, `ResetPasswordCommand`/`Handler`/`Validator`/`Outcome`, 3 new notifications + 3 audit handlers + 1 email handler, `IEmailService.SendPasswordChangedEmailAsync` extension, 13 new handler tests + 2 email handler tests. Run `dotnet test` — confirm `87/87` green (63 baseline + 24 new).
3. **Controller refactor** (Step 6): replace `AuthController.ChangePassword`, `AuthController.ForgotPassword`, `AuthController.ResetPassword` with MediatR senders. Remove the unused `Serilog` `using` and the unused `_renderer` / `_emailOptions` fields (if no longer referenced). Run `dotnet test` — confirm `85/85` green (some controller tests consolidated to MediatR-mock style, net −2).
4. **Docs** (Step 7): update `AGENTS.md` to mark ChangePassword/ForgotPassword/ResetPassword as 🎯.
5. **Rollback**: revert the controller-method changes. The new types stay; they will be reused by the next auth migration.

No DB migration, no config change, no docker compose change.

## Open Questions

None.
