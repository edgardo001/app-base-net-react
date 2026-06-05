## 1. Regla de Oro — Verify baseline

- [ ] 1.1 Run `dotnet test app-base-net-react.slnx --nologo` and confirm 3 existing ConfirmEmail tests pass (Valid/Expired/Invalid token). Target: 88/88.

## 2. Application abstractions

- [ ] 2.1 Create `Application/Common/Models/EmailConfirmationResult.cs` with `EmailErrorCode { None, InvalidConfirmationToken, ConfirmationTokenExpired, UserNotFound }` and `EmailConfirmationResult.Success()` / `.Fail(code, message)`.
- [ ] 2.2 Create `Application/Features/Auth/Commands/ConfirmEmail/ConfirmEmailCommand.cs`: `record ConfirmEmailCommand(string Token, string? IpAddress, string? UserAgent) : IRequest<ConfirmEmailOutcome>`.
- [ ] 2.3 Create `Application/Features/Auth/Commands/ConfirmEmail/ConfirmEmailCommandValidator.cs`: token required, max 256 chars (matches `ResetPasswordCommandValidator`).
- [ ] 2.4 Create `Application/Features/Auth/Commands/ConfirmEmail/ConfirmEmailOutcome.cs`: `record ConfirmEmailOutcome(EmailConfirmationResult Result)`.
- [ ] 2.5 Verify `dotnet build app-base-net-react.slnx` has 0 errors before moving on.

## 3. Notifications + email extension

- [ ] 3.1 Add `EmailConfirmedNotification(Guid UserId, string? IpAddress, string? UserAgent)` record to `Application/Features/Auth/Notifications/AuthNotifications.cs`.
- [ ] 3.2 Add `Task SendWelcomeEmailAsync(Guid userId, CancellationToken ct)` to `IEmailService` in `Application/Common/Interfaces/IServices.cs`.
- [ ] 3.3 Implement `SendWelcomeEmailAsync` in `Infrastructure/Email/EmailService.cs`: look up user via `IUserRepository.GetByIdAsync`, render `welcome.html` template, send via SMTP. Mirror `SendAccountLockedEmailAsync` exactly.
- [ ] 3.4 Create `Infrastructure/Notifications/EmailConfirmedAuditLogHandler.cs`: writes `EmailConfirmed` audit log via `IAuditService.LogAsync`.
- [ ] 3.5 Create `Infrastructure/Notifications/EmailConfirmedEmailHandler.cs`: implements `INotificationHandler<EmailConfirmedNotification>`, looks up user, calls `IEmailService.SendWelcomeEmailAsync(userId, ct)`. Logs warning if user not found.

## 4. Handler + unit tests

- [ ] 4.1 Create `Application/Features/Auth/Commands/ConfirmEmail/ConfirmEmailCommandHandler.cs` implementing `IRequestHandler<ConfirmEmailCommand, ConfirmEmailOutcome>`: token lookup → null check → expiry check → `user.ConfirmEmail()` → `SaveChangesAsync` → `_mediator.Publish(EmailConfirmedNotification)`.
- [ ] 4.2 Create `Application.Tests/Features/Auth/Commands/ConfirmEmail/ConfirmEmailCommandHandlerTests.cs` with 4 tests: `WithValidToken_ConfirmsAndPersistsAndPublishes`, `WithInvalidToken_ReturnsInvalidConfirmationToken`, `WithExpiredToken_ReturnsConfirmationTokenExpired`, `PublishesEmailConfirmedNotificationWithIpAndUserAgent`.
- [ ] 4.3 Create `Application.Tests/Features/Auth/Notifications/EmailConfirmedEmailHandlerTests.cs` with 2 tests: `SendsWelcomeEmail`, `WhenUserNotFound_LogsWarningAndSkips`.
- [ ] 4.4 Run `dotnet test` and confirm all tests pass (target: 88 + 6 = 94).

## 5. Controller refactor

- [ ] 5.1 Refactor `AuthController.ConfirmEmail` (lines 186-212) to: build `ConfirmEmailCommand(request.Token, IpAddress, UserAgent)`, send via `_mediator`, map `EmailErrorCode` → HTTP (`None` → 200, others → 400 with `ErrorMessage`).
- [ ] 5.2 Check if `SendEmail` private helper (lines 214-225) has any other callers in `AuthController`. If not, remove the helper + `_renderer` + `_emailOptions` fields and the `EmailRenderer`/`EmailOptions` constructor params.
- [ ] 5.3 Update the 3 existing ConfirmEmail tests in `AuthControllerTests.cs` (`ConfirmEmail_WithValidToken_...`, `ConfirmEmail_WithExpiredToken_...`, `ConfirmEmail_WithInvalidToken_...`) to mock `IMediator.Send(...)` returning typed `ConfirmEmailOutcome(EmailConfirmationResult)`.
- [ ] 5.4 Add 1 new test: `ConfirmEmail_PassesTokenIpAndUserAgentToHandler` verifying the command payload.
- [ ] 5.5 Run `dotnet test` and confirm all tests pass (target: 94 + 1 = 95; some old tests replaced → final count likely 94).

## 6. Documentation

- [ ] 6.1 Update `AGENTS.md` table: mark ConfirmEmail as ✅ Application-layer (remove the `cqrs-auth-confirm-email` follow-up note).

## 7. Final validation

- [ ] 7.1 Run `openspec validate cqrs-auth-confirm-email --strict` and confirm `"Change 'cqrs-auth-confirm-email' is valid"`.
- [ ] 7.2 Run `dotnet build app-base-net-react.slnx --nologo` and confirm 0 errors, 0 warnings.
- [ ] 7.3 Run `dotnet test app-base-net-react.slnx --nologo` and confirm all tests pass (~94).
- [ ] 7.4 Run `openspec archive cqrs-auth-confirm-email -y` to sync 4 requirements to `openspec/specs/auth-confirm-email/spec.md`. Expect `+ 4 added`.
- [ ] 7.5 Verify archive folder created: `openspec/changes/archive/2026-06-05-cqrs-auth-confirm-email/`.
