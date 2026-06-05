## 1. Baseline — Regla de Oro (tests against the existing controller)

- [x] 1.1 Add 6 new test cases to `AppBaseNetReact.WebApi.Tests/Controllers/AuthControllerTests.cs` covering the Refresh endpoint: valid rotation, unknown token (401), reused/revoked token + all-sessions-revoked (401), expired token (401), user not found (401), inactive user (401)
- [x] 1.2 Add 2 new test cases for the Logout endpoint: known token → revoked + audited, unknown token → 200 no-op
- [x] 1.3 Add the missing mocks to the test constructor for `_uow.RefreshTokens.RevokeAllForUserAsync` and the new `GetActiveByUserAsync` if it gets used (only RevokeAllForUser is used in the reuse-detection path; add the mock only when needed)
- [x] 1.4 Run `dotnet test app-base-net-react.slnx` — confirm `56/56` green before any production code changes

## 2. New abstractions in Application

- [x] 2.1 Create `Application/Common/Models/RefreshResult.cs` with `RefreshErrorCode` enum (`None`, `InvalidToken`, `TokenCompromised`, `TokenExpired`, `UserNotFound`) and `RefreshResult` value object
- [x] 2.2 Create `Application/Features/Auth/Commands/Refresh/RefreshCommand.cs` (record: `RefreshToken`, `IpAddress`, `UserAgent`)
- [x] 2.3 Create `Application/Features/Auth/Commands/Refresh/RefreshResponse.cs` (DTO: `AccessToken`, `RefreshToken`, `ExpiresAt`)
- [x] 2.4 Create `Application/Features/Auth/Commands/Refresh/RefreshOutcome.cs` (`record RefreshOutcome(RefreshResult Result, RefreshResponse? Response)`)
- [x] 2.5 Create `Application/Features/Auth/Commands/Refresh/RefreshCommandValidator.cs` (`AbstractValidator<RefreshCommand>` with `NotEmpty` on `RefreshToken`)
- [x] 2.6 Create `Application/Features/Auth/Commands/Logout/LogoutCommand.cs` (record: `RefreshToken`, `IpAddress`, `UserAgent`)
- [x] 2.7 Create `Application/Features/Auth/Commands/Logout/LogoutCommandValidator.cs` (`AbstractValidator<LogoutCommand>` with `NotEmpty` on `RefreshToken`)

## 3. Notifications and Infrastructure handlers

- [x] 3.1 Extend `Application/Features/Auth/Notifications/AuthNotifications.cs` with 3 new `INotification` records: `TokenRefreshedNotification`, `TokenReuseDetectedNotification`, `UserLoggedOutNotification`
- [x] 3.2 Create `Infrastructure/Notifications/TokenRefreshedAuditHandler.cs` (`INotificationHandler<TokenRefreshedNotification>` → `IAuditService.LogAsync("TokenRefreshed", "RefreshToken", tokenId, ...)`)
- [x] 3.3 Create `Infrastructure/Notifications/TokenReuseDetectedAuditHandler.cs` (`INotificationHandler<TokenReuseDetectedNotification>` → `IAuditService.LogAsync("TokenReuseDetected", "RefreshToken", tokenId, ...)`)
- [x] 3.4 Create `Infrastructure/Notifications/UserLoggedOutAuditHandler.cs` (`INotificationHandler<UserLoggedOutNotification>` → `IAuditService.LogAsync("UserLoggedOut", "RefreshToken", tokenId, ...)`)
- [x] 3.5 Confirm `Application/DependencyInjection.cs` `AddMediatR(...).RegisterServicesFromAssembly(...)` auto-picks up the 3 new notification handlers — no manual DI change required

## 4. Handlers + unit tests

- [x] 4.1 Create `Application/Features/Auth/Commands/Refresh/RefreshCommandHandler.cs` implementing the 6 scenarios — see `design.md` §"Decisions" for the orchestration
- [x] 4.2 Create `AppBaseNetReact.Application.Tests/Features/Auth/Commands/Refresh/RefreshCommandHandlerTests.cs` with 6 tests mirroring the 6 controller Refresh tests, asserting on `RefreshOutcome` directly
- [x] 4.3 Create `Application/Features/Auth/Commands/Logout/LogoutCommandHandler.cs` (returns `Unit`; revokes + publishes notification if token exists, otherwise no-op)
- [x] 4.4 Create `AppBaseNetReact.Application.Tests/Features/Auth/Commands/Logout/LogoutCommandHandlerTests.cs` with 2 tests (known token + unknown token)
- [x] 4.5 Run `dotnet test app-base-net-react.slnx` — confirm `64/64` green (56 baseline + 8 new handler tests; controller tests still passing against the *old* code path)

## 5. Refactor AuthController.Refresh + AuthController.Logout to use MediatR

- [x] 5.1 Replace the body of `AuthController.Refresh` (lines 96–149) with a MediatR sender: build `RefreshCommand` from the request + `HttpContext`; call `IMediator.Send`; switch on `RefreshErrorCode` to map to `Unauthorized(ApiResponse<object>.Fail(message))` for all failure cases, or `Ok(ApiResponse<object>.Ok(new { AccessToken, RefreshToken, ExpiresAt }))` on success
- [x] 5.2 Replace the body of `AuthController.Logout` (lines 151–171) with a MediatR sender: build `LogoutCommand`; call `IMediator.Send`; return `Ok(ApiResponse<object>.Ok(null, "Logged out successfully"))`
- [x] 5.3 Remove the `_audit` field from `AuthController` ONLY if it is no longer used by `ChangePassword`/`ResetPassword`/`ConfirmEmail` (it is — keep it). Remove `_jwt` field ONLY if it is no longer used (it is — keep it). Remove `GetUserPermissions` private helper if no longer used (it is used by Refresh's replacement path? No — the new handler uses inline permission extraction. Check after the refactor.)
- [x] 5.4 Run `dotnet test app-base-net-react.slnx` — confirm `64/64` still green
- [x] 5.5 Run `dotnet build app-base-net-react.slnx` — confirm `0` errors and no new warnings

## 6. Documentation update

- [x] 6.1 Update `AGENTS.md` "¿Dónde ocurre la acción?" table — replace the single "Refresh/Logout/ChangePassword/Forgot/Reset/ConfirmEmail" row with two: "Refresh + Logout" → 🎯 (CQRS), and the remaining three (ChangePassword/ForgotPassword/ResetPassword/ConfirmEmail) → ⚡ (still controller-orchestrated, will be migrated as follow-up)
- [x] 6.2 Add a small note in the same table cell: "Migrated in `openspec/changes/cqrs-auth-refresh/`"

## 7. Final validation

- [x] 7.1 `dotnet test app-base-net-react.slnx` → `63/63` green (38 Application + 25 WebApi)
- [x] 7.2 `dotnet build app-base-net-react.slnx` → success, no new warnings
- [x] 7.3 `openspec validate cqrs-auth-refresh --strict` → passes
- [x] 7.4 No DB migration needed; no config changes; no docker compose changes
