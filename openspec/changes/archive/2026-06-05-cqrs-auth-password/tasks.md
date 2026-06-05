## 1. Baseline — Regla de Oro (tests against the existing controller)

- [x] 1.1 Add 3 new test cases to `AppBaseNetReact.WebApi.Tests/Controllers/AuthControllerTests.cs` covering the `ChangePassword` endpoint: valid change returns 200 + audit + email; wrong current password returns 400; weak new password returns 400
- [x] 1.2 Add 2 new test cases for the `ForgotPassword` endpoint: registered email returns 200 + audit + email; unregistered email returns 200 with no side effects (2 tests already existed; verified they pass)
- [x] 1.3 Add 4 new test cases for the `ResetPassword` endpoint: valid token returns 200 + password updated + audit + email; unknown token returns 400; expired token returns 400; weak new password returns 400
- [x] 1.4 Run `dotnet test app-base-net-react.slnx` — confirm `70/70` green before any production code changes

## 2. New abstractions in Application

- [x] 2.1 Create `Application/Common/Models/PasswordResult.cs` with `PasswordErrorCode` enum (`None`, `InvalidCurrentPassword`, `WeakPassword`, `InvalidResetToken`, `ResetTokenExpired`, `UserNotFound`) and `PasswordResult` value object
- [x] 2.2 Create `Application/Features/Auth/Commands/ChangePassword/ChangePasswordCommand.cs` (record: `Guid UserId`, `string CurrentPassword`, `string NewPassword`, `string? IpAddress`, `string? UserAgent`)
- [x] 2.3 Create `Application/Features/Auth/Commands/ChangePassword/ChangePasswordOutcome.cs` (`record ChangePasswordOutcome(PasswordResult Result)`)
- [x] 2.4 Create `Application/Features/Auth/Commands/ChangePassword/ChangePasswordCommandValidator.cs` (`AbstractValidator<ChangePasswordCommand>` with `NotEmpty` on `CurrentPassword` and `NewPassword`)
- [x] 2.5 Create `Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs` (record: `string Email`, `string? IpAddress`, `string? UserAgent`)
- [x] 2.6 Create `Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordOutcome.cs` (`record ForgotPasswordOutcome(PasswordResult Result)`)
- [x] 2.7 Create `Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommandValidator.cs` (`AbstractValidator<ForgotPasswordCommand>` with `NotEmpty().EmailAddress().MaximumLength(256)` on `Email`)
- [x] 2.8 Create `Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommand.cs` (record: `string Token`, `string NewPassword`, `string? IpAddress`, `string? UserAgent`)
- [x] 2.9 Create `Application/Features/Auth/Commands/ResetPassword/ResetPasswordOutcome.cs` (`record ResetPasswordOutcome(PasswordResult Result)`)
- [x] 2.10 Create `Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommandValidator.cs` (`AbstractValidator<ResetPasswordCommand>` with `NotEmpty` on `Token` and `NewPassword`)

## 3. Notifications and Infrastructure handlers

- [x] 3.1 Extend `Application/Features/Auth/Notifications/AuthNotifications.cs` with 3 new `INotification` records: `PasswordChangedNotification(UserId, Email, FirstName, IpAddress, UserAgent)`, `PasswordResetRequestedNotification(UserId, Email, FirstName, IpAddress, UserAgent)`, `PasswordResetNotification(UserId, Email, FirstName, IpAddress, UserAgent)`
- [x] 3.2 Add `IEmailService.SendPasswordChangedEmailAsync(string email, string firstName, CancellationToken ct)` to `Application/Common/Interfaces/IServices.cs` (mirrors the `SendAccountLockedEmailAsync` pattern)
- [x] 3.3 Implement `SendPasswordChangedEmailAsync` in `Infrastructure/Email/EmailService.cs` (lookup `EmailOptions.Templates["PasswordChanged"]`, render via `EmailRenderer`, send via SMTP)
- [x] 3.4 Create `Infrastructure/Notifications/PasswordChangedAuditHandler.cs` (`INotificationHandler<PasswordChangedNotification>` → `IAuditService.LogAsync("PasswordChanged", "User", userId, ...)`)
- [x] 3.5 Create `Infrastructure/Notifications/PasswordResetRequestedAuditHandler.cs` (`INotificationHandler<PasswordResetRequestedNotification>` → `IAuditService.LogAsync("PasswordResetRequested", "User", userId, ...)` with `Details="Reset token generated"`)
- [x] 3.6 Create `Infrastructure/Notifications/PasswordResetAuditHandler.cs` (`INotificationHandler<PasswordResetNotification>` → `IAuditService.LogAsync("PasswordReset", "User", userId, ...)` with `Details="Password reset via token"`)
- [x] 3.7 Create `Infrastructure/Notifications/SendPasswordChangedEmailHandler.cs` (`INotificationHandler<PasswordChangedNotification>` + `INotificationHandler<PasswordResetNotification>` → `IEmailService.SendPasswordChangedEmailAsync`, with try/catch + `ILogger<T>.LogError`)
- [x] 3.8 Confirm `Application/DependencyInjection.cs` `AddMediatR(...).RegisterServicesFromAssembly(...)` auto-picks up the 4 new notification handlers — no manual DI change required

## 4. Handlers + unit tests

- [x] 4.1 Create `Application/Features/Auth/Commands/ChangePassword/ChangePasswordCommandHandler.cs` implementing the 6 scenarios — see `design.md` §"Decisions" for the orchestration (verify current pwd → validate new pwd → set hash → revoke all → save → publish)
- [x] 4.2 Create `AppBaseNetReact.Application.Tests/Features/Auth/Commands/ChangePassword/ChangePasswordCommandHandlerTests.cs` with 6 tests: happy path, user not found, wrong current pwd, weak new pwd, audit published, email notification published
- [x] 4.3 Create `Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs` implementing the 3 scenarios — see `design.md` §"Decisions" (anti-enumeration: always success, branch internally)
- [x] 4.4 Create `AppBaseNetReact.Application.Tests/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandlerTests.cs` with 3 tests: registered user → success + notification published, unregistered user → success + NO notification published, no side effects on unknown email
- [x] 4.5 Create `Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommandHandler.cs` implementing the 5 scenarios — see `design.md` §"Decisions" (token lookup, expiry check, weak-password check, atomic update with ForcePasswordChange + ConfirmEmail, publish)
- [x] 4.6 Create `AppBaseNetReact.Application.Tests/Features/Auth/Commands/ResetPassword/ResetPasswordCommandHandlerTests.cs` with 5 tests: happy path, unknown token, expired token, weak pwd, audit + email published
- [x] 4.7 Create `AppBaseNetReact.Application.Tests/Features/Auth/Notifications/SendPasswordChangedEmailHandlerTests.cs` with 3 tests: success path for PasswordChanged, success path for PasswordReset, failure path (exception swallowed, logger receives error)
- [x] 4.8 Run `dotnet test app-base-net-react.slnx` — confirm `87/87` green (70 baseline + 17 new handler tests)

## 5. Refactor AuthController.ChangePassword + ForgotPassword + ResetPassword to use MediatR

- [ ] 5.1 Replace the body of `AuthController.ChangePassword` (lines 132–173) with a MediatR sender: read `User.FindFirst("sub")` and return 401 if missing/malformed; call `Users.GetByIdAsync` and return 404 if null; build `ChangePasswordCommand(UserId, CurrentPassword, NewPassword, Ip, UA)`; call `IMediator.Send`; map `PasswordErrorCode` to `BadRequest(ApiResponse.Fail(message))` for `InvalidCurrentPassword`/`WeakPassword`, `NotFound()` for `UserNotFound`, or `Ok(ApiResponse.Ok(null, "Password changed successfully"))` on success
- [ ] 5.2 Replace the body of `AuthController.ForgotPassword` (lines 175–203) with a MediatR sender: build `ForgotPasswordCommand(Email, Ip, UA)`; call `IMediator.Send`; always return `Ok(ApiResponse.Ok(null, "If the email exists, a password reset link has been sent."))`
- [ ] 5.3 Replace the body of `AuthController.ResetPassword` (lines 205–236) with a MediatR sender: build `ResetPasswordCommand(Token, NewPassword, Ip, UA)`; call `IMediator.Send`; map `PasswordErrorCode` to `BadRequest(ApiResponse.Fail(message))` for `InvalidResetToken`/`ResetTokenExpired`/`WeakPassword`, or `Ok(ApiResponse.Ok(null, "Password reset successfully"))` on success
- [ ] 5.4 Remove the `_renderer`, `_emailOptions`, and `_frontendUrl` private fields from `AuthController` ONLY if they are no longer used by the remaining `ConfirmEmail` endpoint (they are — keep them for now). Remove the `Serilog` `using` (the only `Log.Warning` call was in the change-password email fallback, now in the notification handler). Remove the private `SendEmail` helper if no other endpoint uses it (ConfirmEmail still uses it — keep it for now).
- [ ] 5.5 Update the 9 controller tests in `AuthControllerTests.cs` to mock `IMediator.Send` for the 3 endpoints (drop the now-unused mocks for `_uow.Users`, `_uow.RefreshTokens.RevokeAllForUserAsync`, `_audit`, `_email` on those paths). Expect ~ −2 net tests as some happy paths get consolidated.
- [ ] 5.6 Run `dotnet test app-base-net-react.slnx` — confirm `85/85` still green
- [ ] 5.7 Run `dotnet build app-base-net-react.slnx` — confirm `0` errors and no new warnings

## 6. Documentation update

- [ ] 6.1 Update `AGENTS.md` "¿Dónde ocurre la acción?" table — replace the "ChangePassword/ForgotPassword/ResetPassword/ConfirmEmail" row with: "ChangePassword + ForgotPassword + ResetPassword" → 🎯 (CQRS, migrated in this change), and a new "ConfirmEmail" → ⚡ row (will be migrated as follow-up)
- [ ] 6.2 Add a small note in the migrated cell: "Migrated in `openspec/changes/cqrs-auth-password/`"

## 7. Final validation

- [ ] 7.1 `dotnet test app-base-net-react.slnx` → `85/85` green
- [ ] 7.2 `dotnet build app-base-net-react.slnx` → success, no new warnings
- [ ] 7.3 `openspec validate cqrs-auth-password --strict` → passes
- [ ] 7.4 No DB migration needed; no config changes; no docker compose changes
