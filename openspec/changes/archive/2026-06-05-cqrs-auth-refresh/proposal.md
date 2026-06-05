## Why

The first CQRS migration (`cqrs-auth-login`) established the conventions for moving auth flows from controller-orchestrated to MediatR-based. The two remaining auth flows that share the same architecture shape are `Refresh` and `Logout`: both run inside the `AuthController`, both touch the refresh-token store, both have audit side effects, and both have security-critical paths (token-reuse detection in Refresh). They are migrated together because:

1. They share the same `RefreshRequest` DTO and the same `JwtId`-based lookup pattern.
2. `Refresh` is the next-most-trafficked endpoint after `Login` (every expired access token uses it).
3. `Logout` is small enough that bundling it adds zero risk and saves a follow-up change.

Like the Login change, this one preserves the HTTP contract bit-for-bit: same status codes (`200`, `401`), same response shapes, same side effects (token rotation, reuse detection, audit, revocation of all sessions on detected compromise).

## What Changes

- **Add** `Application/Features/Auth/Commands/Refresh/` with `RefreshCommand` (record implementing `IRequest<RefreshOutcome>`), `RefreshResponse` (DTO: `AccessToken`, `RefreshToken`, `ExpiresAt`), `RefreshOutcome` (result wrapper with `RefreshResult` + optional `RefreshResponse`), `RefreshCommandValidator`.
- **Add** `RefreshCommandHandler` that:
  - Hashes the incoming refresh token, looks up the stored token by hash.
  - Returns failure when the token is missing, revoked (→ revokes ALL the user's sessions + emits `TokenReuseDetectedNotification`), expired, or when the user is missing/inactive.
  - On success: revokes the old token, generates a new access + refresh token pair, persists the new refresh token, and publishes `TokenRefreshedNotification`.
  - Does **not** know about `HttpContext`, `IPasswordPolicyService`, `IPasswordHasherService`, or `IConfiguration`.
- **Add** `Application/Features/Auth/Commands/Logout/` with `LogoutCommand`, `LogoutCommandValidator`, `LogoutCommandHandler`. The handler is the simplest in the auth flow — it just revokes the stored token (if any) and publishes `UserLoggedOutNotification`. Always returns success (no error states) to match the current behavior, which never reveals whether a token existed.
- **Add** `Application/Common/Models/RefreshResult.cs` with `RefreshErrorCode` enum (`None`, `InvalidToken`, `TokenCompromised`, `TokenExpired`, `UserNotFound`) and a `RefreshResult` value object (mirrors the `LoginResult` shape from `cqrs-auth-login`).
- **Add** 3 new `INotification` records to `Application/Features/Auth/Notifications/AuthNotifications.cs`: `TokenReuseDetectedNotification`, `TokenRefreshedNotification`, `UserLoggedOutNotification`.
- **Add** 3 new notification handlers in `Infrastructure/Notifications/`:
  - `TokenReuseDetectedAuditHandler` — writes the `TokenReuseDetected` audit log entry (the current "all sessions revoked" audit).
  - `TokenRefreshedAuditHandler` — writes the `TokenRefreshed` audit log entry (currently NOT audited — this is a small new observability win).
  - `UserLoggedOutAuditHandler` — writes the `UserLoggedOut` audit log entry.
- **Refactor** `AuthController.Refresh` to a thin MediatR sender: build `RefreshCommand` from the request, call `IMediator.Send`, map the `RefreshOutcome` to the existing `ApiResponse<T>` + `IActionResult` (always 200/401, same as today).
- **Refactor** `AuthController.Logout` to a thin MediatR sender: build `LogoutCommand`, call `IMediator.Send`, return `Ok(ApiResponse<object>.Ok(null, "Logged out successfully"))`.
- **Remove** the now-unused `_audit` and `_jwt` private fields from `AuthController` ONLY if they become unused after this change (they are still used by `ChangePassword` and `ResetPassword` for the audit log, and by `ChangePassword` for the password-changed email — keep them if still used).
- **Add** `RefreshCommandHandlerTests` in `AppBaseNetReact.Application.Tests/Features/Auth/Commands/Refresh/` with 6 scenarios (valid rotation, invalid token, revoked token + all-sessions-revoked, expired token, user not found, inactive user).
- **Add** `LogoutCommandHandlerTests` with 2 scenarios (token found → revoked + audited, token not found → no-op success).
- **Add** 6 new tests to `AuthControllerTests` (Regla de Oro) covering all Refresh scenarios **before** the refactor. 2 new tests for Logout. Existing controller test count: 14 → **22**.
- **Update** `AGENTS.md` "Where does the action happen?" table: Refresh + Logout move to the 🎯 (CQRS) column.

## Capabilities

### New Capabilities
- `auth-refresh`: Vertical slice covering `POST /api/auth/refresh` — token rotation, reuse detection (revoke-all-sessions), expiry check, user-active check.
- `auth-logout`: Vertical slice covering `POST /api/auth/logout` — single-token revocation, audit, idempotent (no-op on missing token).

### Modified Capabilities
(none — `auth-refresh` and `auth-logout` are new capabilities; the existing `auth-login` capability is unchanged.)

## Impact

- **Backend**:
  - New files in `Application/Features/Auth/Commands/Refresh/` (5 files) and `Application/Features/Auth/Commands/Logout/` (3 files).
  - New file: `Application/Common/Models/RefreshResult.cs`.
  - New files in `Infrastructure/Notifications/` (3 files).
  - Modified: `AuthController.cs` (Refresh and Logout methods shrink to ~10 lines each), `AuthControllerTests.cs` (+8 tests, refactored mocks), `AuthNotifications.cs` (+3 notifications), `AGENTS.md`.
  - **No breaking change** to HTTP contract. Refresh returns the same shape and status codes. Logout still returns 200 even when the token does not exist.
  - **No DB migration**. No entity changes.
- **Frontend**: none.
- **Config**: none.
- **Tests**: 48 → 64 (+8 controller baseline + 8 handler).
- **New observability**: every successful Refresh now writes a `TokenRefreshed` audit entry (previously the audit log only captured login/logout/reuse events, not routine refresh).
