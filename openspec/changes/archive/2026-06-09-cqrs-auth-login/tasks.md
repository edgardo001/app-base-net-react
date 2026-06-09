## 1. Baseline — Regla de Oro (tests against the existing controller)

- [x] 1.1 Add 8 new test cases to `AppBaseNetReact.WebApi.Tests/Controllers/AuthControllerTests.cs` covering: valid credentials, unknown email, invalid password, deactivated account, locked account (423), unconfirmed email (403), lockout-threshold + email sent, and password-expired flag
- [x] 1.2 Set up the shared mocks in the test constructor for `_uow.LoginAttempts.AddAsync`, `_uow.RefreshTokens.AddAsync`, and `_uow.SaveChangesAsync` (used by the new test paths)
- [x] 1.3 Run `dotnet test app-base-net-react.slnx` — confirm baseline is `40/40` green before any production code changes

## 2. New abstractions in Application

- [x] 2.1 Create `Application/Common/Models/LoginResult.cs` with the `LoginErrorCode` enum and `LoginResult` value object (`Success()` / `Fail(code, message, remainingLockoutMinutes?)`)
- [x] 2.2 Create `Application/Features/Auth/Commands/Login/LoginCommand.cs` (record implementing `IRequest<LoginOutcome>` with `Email`, `Password`, `IpAddress`, `UserAgent`, `FrontendUrl`)
- [x] 2.3 Create `Application/Features/Auth/Commands/Login/LoginResponse.cs` (DTO: `AccessToken`, `RefreshToken`, `ExpiresAt`, `UserId`, `Email`, `FirstName`, `LastName`, `AvatarPath`, `Permissions`, `PasswordExpired`)
- [x] 2.4 Create `Application/Features/Auth/Commands/Login/LoginOutcome.cs` (`record LoginOutcome(LoginResult Result, LoginResponse? Response)`)
- [x] 2.5 Create `Application/Features/Auth/Commands/Login/LoginCommandValidator.cs` (`AbstractValidator<LoginCommand>` with `NotEmpty` + `EmailAddress` + `MaximumLength(256)` for email, `NotEmpty` for password)

## 3. Notifications and Infrastructure handlers

- [x] 3.1 Create `Application/Features/Auth/Notifications/AuthNotifications.cs` with the 3 `INotification` records: `UserLoggedInNotification`, `UserLoginFailedNotification`, `AccountLockedNotification`
- [x] 3.2 Extend `IEmailService` (in `Application/Common/Interfaces/IServices.cs`) with `SendAccountLockedEmailAsync(string to, string userName, int lockoutMinutes, string resetLink, CancellationToken ct = default)`
- [x] 3.3 Implement `SendAccountLockedEmailAsync` in `Infrastructure/Email/EmailService.cs` using the existing `AccountLocked` template (reads `_options.Templates["AccountLocked"]`, renders via `EmailRenderer`, calls `SendEmailAsync`)
- [x] 3.4 Create `Infrastructure/Notifications/UserLoggedInAuditHandler.cs` (`INotificationHandler<UserLoggedInNotification>` that calls `IAuditService.LogAsync("UserLoggedIn", "User", userId, ...)`)
- [x] 3.5 Create `Infrastructure/Notifications/UserLoginFailedAuditHandler.cs` (`INotificationHandler<UserLoginFailedNotification>` that calls `IAuditService.LogAsync("UserLoginFailed", null, ...)`)
- [x] 3.6 Create `Infrastructure/Notifications/AccountLockedEmailHandler.cs` (`INotificationHandler<AccountLockedNotification>` that calls `IEmailService.SendAccountLockedEmailAsync` inside a try/catch with a logger warning on failure)
- [x] 3.7 Confirm `Application/DependencyInjection.cs` `AddMediatR(...).RegisterServicesFromAssembly(...)` will auto-pick up the 3 new notification handlers — no manual DI change required

## 4. LoginCommandHandler + unit tests

- [x] 4.1 Create `Application/Features/Auth/Commands/Login/LoginCommandHandler.cs` (`IRequestHandler<LoginCommand, LoginOutcome>`) implementing the 8 scenarios from the spec — see `design.md` §"Decisions" for the exact orchestration
- [x] 4.2 Create `AppBaseNetReact.Application.Tests/Features/Auth/Commands/Login/LoginCommandHandlerTests.cs` with 8 tests mirroring the 8 controller tests, but asserting on the `LoginOutcome` directly (no HTTP layer)
- [x] 4.3 Run `dotnet test app-base-net-react.slnx` — confirm `48/48` green (40 baseline + 8 new handler tests; controller tests are still passing against the *old* code path)

## 5. Refactor AuthController.Login to use MediatR

- [x] 5.1 Replace the body of `AuthController.Login` (lines 57–133) with a MediatR sender: build `LoginCommand` from the request + `HttpContext` (IP, UserAgent) + `IConfiguration["FrontendUrl"]`; call `IMediator.Send(command, ct)`; switch on `outcome.Result.ErrorCode` to map to `Ok(ApiResponse<…>)/Unauthorized/StatusCode(403)/StatusCode(423)` using the same messages and shape as today
- [x] 5.2 Remove the now-unused private helpers `GetUserPermissions`, `LogLoginAttempt`, `SendEmail`, `SendAccountLockedEmail`, `GenerateToken` from `AuthController` (only the Login-specific ones; keep the ones used by other endpoints)
- [x] 5.3 Remove unused constructor parameters from `AuthController` for the Login path (audit, email, renderer, options, frontendUrl, policy are still used by other endpoints — keep them)
- [x] 5.4 Run `dotnet test app-base-net-react.slnx` — confirm `48/48` still green (the 8 controller tests now exercise the MediatR path; the 8 handler tests prove the handler is correct)
- [x] 5.5 Run `dotnet build app-base-net-react.slnx` and confirm `0` errors and `0` warnings (the existing `CS8625` warnings on `null` literals are unrelated and may remain)

## 6. Documentation update

- [x] 6.1 Update `AGENTS.md` §"Diagrama de Arquitectura" — mark the Login flow as 🎯 (CQRS) instead of ⚡ (controller-orchestrated). The Target/Actual diff for Login disappears; the rest of the controllers stay in the ⚡ column.
- [x] 6.2 Update the "Flujo de Ejecución — Situación Actual vs Target" section in `AGENTS.md` to add a small note: "Login migrated to CQRS in `cqrs-auth-login` change" with a one-line cross-reference to the change directory

## 7. Final validation

- [x] 7.1 `dotnet test app-base-net-react.slnx` → `48/48` green
- [x] 7.2 `dotnet build app-base-net-react.slnx` → success, no new warnings
- [x] 7.3 `openspec validate cqrs-auth-login --strict` → passes (proposal + spec + design + tasks all align)
- [x] 7.4 No DB migration needed; no config changes; no docker compose changes
