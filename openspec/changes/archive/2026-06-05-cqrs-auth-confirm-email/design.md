## Context

`AuthController.ConfirmEmail` is the last ⚡ auth flow. It currently:
1. Calls `_uow.Users.GetByEmailConfirmationTokenAsync(token, ct)`.
2. Returns 400 on null user.
3. Returns 400 on expired token.
4. Calls `user.ConfirmEmail()` (domain mutation).
5. Persists via `_uow.SaveChangesAsync(ct)`.
6. Writes audit log via `_audit.LogAsync("EmailConfirmed", ...)`.
7. Sends welcome email via the private `SendEmail(user, "Welcome", ...)` helper.
8. Returns 200 with `ApiResponse<object>.Ok(null, "Email confirmed successfully")`.

The established CQRS pattern (login, refresh, password) is:
- Controller reads HttpContext, builds Command, sends to MediatR.
- Handler does lookups + domain mutation + persistence + publishes notification.
- Notification handlers (in Infrastructure) do side-effects: audit + email.
- 2 test layers: controller maps Command → HTTP; handler unit tests cover orchestration.

A subtle constraint: the **same `User.EmailConfirmationToken` field is shared with `ResetPassword`** (designed in `cqrs-auth-password`). Token generation/expiry semantics are shared, but the *consumption* is per-flow. `ConfirmEmail` only sets `EmailConfirmed = true`; it does NOT clear the token (consistent with prior behavior).

## Goals / Non-Goals

**Goals:**
- Migrate `ConfirmEmail` to MediatR; preserve HTTP contract bit-for-bit.
- Add `SendWelcomeEmailAsync` to `IEmailService` for symmetry with `SendAccountLockedEmailAsync` / `SendPasswordChangedEmailAsync`.
- 2-layer test coverage: controller mapping + handler orchestration + email handler.
- Application layer stays free of `HttpContext`, `IConfiguration`, `EmailRenderer`, `EmailOptions`.

**Non-Goals:**
- Token generation/rotation changes (handled by `cqrs-auth-password`/future change).
- HTTP rate limiting on `confirm-email` (not in current scope).
- Welcome email templating changes (template file unchanged).
- Refactor of `SendAccountLockedEmail` private helper (still used by `LoginHandler` via `AccountLockedNotification`).

## Decisions

### D1. Shared `EmailConfirmationResult` model
- New `EmailConfirmationResult` + `EmailErrorCode { None, InvalidConfirmationToken, ConfirmationTokenExpired, UserNotFound }` in `Application/Common/Models/`.
- Mirrors `PasswordResult` pattern. Note: `UserNotFound` is included for symmetry but **not reachable** in this flow (token lookup either returns a user or null with a token-shaped error).
- `ConfirmEmailOutcome(PasswordResult Result)` — wait, no, use `ConfirmEmailOutcome(EmailConfirmationResult Result)`.

### D2. `EmailConfirmedNotification` payload
- `record EmailConfirmedNotification(Guid UserId, string? IpAddress, string? UserAgent)` — same shape as `PasswordChangedNotification` (no token echoed in notifications to avoid accidental logging).
- Carries only the IDs/context needed by audit + email handlers.

### D3. Two notification handlers
- `EmailConfirmedAuditLogHandler` writes `EmailConfirmed` action via `IAuditService` (mirrors `LoginAuditLogHandler`, `RefreshAuditLogHandler`, `PasswordChangedAuditLogHandler`).
- `EmailConfirmedEmailHandler` calls `IEmailService.SendWelcomeEmailAsync(user)` (mirrors `SendPasswordChangedEmailHandler`).
- `EmailConfirmedEmailHandler` is also registered for `IRequestHandler<SendWelcomeEmailCommand, Unit>` style? **No** — just `INotificationHandler<EmailConfirmedNotification>`. It looks up the user via `IUserRepository.GetByIdAsync(userId)` so it can render the template with the user's name and email.

### D4. `IEmailService.SendWelcomeEmailAsync` signature
- `Task SendWelcomeEmailAsync(Guid userId, CancellationToken ct)` — takes userId, not user entity, so the handler can be unit-tested without mocking the email body.
- The email service looks up the user, renders `welcome.html`, calls `SendEmailAsync`.
- Mirrors `SendAccountLockedEmailAsync(Guid userId, int lockoutMinutes, ct)` shape.

### D5. Handler: single `SaveChangesAsync`
- `ConfirmEmailCommandHandler.Handle`:
  1. `_uow.Users.GetByEmailConfirmationTokenAsync(command.Token, ct)` → null → `EmailErrorCode.InvalidConfirmationToken`.
  2. Check `user.EmailConfirmationTokenExpires < _clock.UtcNow` → expired → `ConfirmationTokenExpired`.
  3. `user.ConfirmEmail()` (domain mutation).
  4. `_uow.SaveChangesAsync(ct)`.
  5. Publish `EmailConfirmedNotification(user.Id, command.IpAddress, command.UserAgent)` via `_mediator.Publish`.
  6. Return `EmailConfirmationResult.Success()`.
- No `try/catch` around email; MediatR's `Publish` swallows handler exceptions in our setup (matches existing `LoginHandler`).

### D6. Controller mapping
- `AuthController.ConfirmEmail`:
  1. Build `ConfirmEmailCommand(request.Token, IpAddress, UserAgent)`.
  2. `await _mediator.Send(command, ct)`.
  3. Map `EmailErrorCode.None` → 200, otherwise → 400 with `outcome.Result.ErrorMessage`.
- No 404 path (not used; null token is "Invalid confirmation token", not "User not found").

### D7. Field cleanup in `AuthController`
- After this migration, `_renderer` and `_emailOptions` are still used by the private `SendEmail` helper, but `ConfirmEmail` is the only call site.
- If `SendEmail` becomes unused (no other callers in controller), both fields + helper can be removed. **Defer this decision** to §5 implementation; check actual usage first.

### D8. Test strategy
- **Baseline first (Regla de Oro)**: 3 existing tests (`ConfirmEmail_WithValidToken_...`, `ConfirmEmail_WithExpiredToken_...`, `ConfirmEmail_WithInvalidToken_...`) cover the original controller behavior. Keep these as-is for now; refactor in §5 to mock `IMediator` returning typed outcomes.
- **Handler tests** in `Application.Tests`:
  - `ConfirmEmail_WithValidToken_ConfirmsAndPersistsAndPublishesNotification`
  - `ConfirmEmail_WithInvalidToken_ReturnsInvalidConfirmationToken`
  - `ConfirmEmail_WithExpiredToken_ReturnsConfirmationTokenExpired`
  - `ConfirmEmail_PublishesEmailConfirmedNotificationWithIpAndUserAgent`
- **Email handler tests**:
  - `EmailConfirmedEmailHandler_SendsWelcomeEmail`
  - `EmailConfirmedEmailHandler_WhenUserNotFound_LogsWarningAndSkips` (defensive)

## Risks / Trade-offs

- **[Risk] `ConfirmEmail` does not clear `EmailConfirmationToken` on success** → could allow re-confirmation, but this preserves current behavior. → Document in design; defer to a future token-rotation change.
- **[Risk] Welcome email is sent via `SendEmail` helper, which is private to controller** → moving to `IEmailService` is a small risk if `EmailRenderer` lookup logic differs. → Mirror `SendAccountLockedEmailAsync` exactly; reuse `EmailRenderer` and `EmailOptions` resolution.
- **[Risk] `MediatR.Publish` semantics**: if a notification handler throws, the parent handler's `SaveChanges` has already committed. → Consistent with existing login/refresh/password flows; matches "audit + email are best-effort side-effects" pattern. The user is still confirmed even if the email fails.

## Migration Plan

1. Add 7 § tasks to `tasks.md`:
   - §1 Regla de Oro: verify 3 baseline tests pass (they already exist from the `cqrs-auth-password` session).
   - §2 Add `EmailConfirmationResult` model + 3 application-layer files.
   - §3 Add `EmailConfirmedNotification` + 2 handlers + `SendWelcomeEmailAsync` interface + impl.
   - §4 Write handler/email-handler unit tests (target ~5 new tests).
   - §5 Refactor `AuthController.ConfirmEmail` + update 3 tests.
   - §6 Update `AGENTS.md` table.
   - §7 Final validation: `openspec validate --strict`, `dotnet test`, `dotnet build` (0/0), archive.
2. Rollback: revert commit. The migration is a pure refactor with no schema or HTTP-contract changes.
3. Deploy: no special steps; backend-only change.

## Open Questions

- None. All decisions are consistent with the established `cqrs-auth-password` and `cqrs-auth-refresh` patterns.
