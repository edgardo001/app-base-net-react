## Context

The `cqrs-auth-login` change established the CQRS conventions for the auth surface: typed `*Result` + `*Outcome` records, MediatR `INotification` fan-out for audit/email, `IRequestHandler<,>` handlers in the Application layer, and a thin controller that maps `*ErrorCode` to `IActionResult` status codes. This change extends the same pattern to `Refresh` and `Logout`, the next-most-trafficked auth flows.

The current `AuthController.Refresh` and `AuthController.Logout` together account for ~50 lines of inline orchestration: token hashing, repository lookups, reuse-detection (a security-critical path), expiry checks, user-active checks, token rotation, and audit logging. They are migrated together because:

1. They share the same DTO (`RefreshRequest(string RefreshToken)`) and the same `IUnitOfWork.RefreshTokens` repository.
2. The migration of one without the other would still leave one half-orchestrated, half-CQRS — worse for readability than the current state.
3. The patterns and tests are nearly identical: a `RefreshCommand` with 5 failure modes vs. a `LogoutCommand` with 0 failure modes.

`Refresh` is also where **token-reuse detection** lives — when a previously-revoked refresh token is presented, the system revokes ALL the user's sessions. This is the most security-sensitive path in the auth flow, and it benefits from being testable in isolation (today the only coverage of the reuse path is end-to-end).

## Goals / Non-Goals

**Goals:**
- Move `AuthController.Refresh` and `AuthController.Logout` orchestration into `RefreshCommandHandler` and `LogoutCommandHandler` in the Application layer.
- Preserve the HTTP contract bit-for-bit: same status codes (`200` / `401`), same response shape, same side effects (token rotation, reuse detection, audit).
- Reuse the `LoginResult`/`LoginOutcome` shape via a new `RefreshResult` + `RefreshOutcome` (with its own `RefreshErrorCode` enum).
- Add a small new observability win: every successful Refresh now writes a `TokenRefreshed` audit row (currently only login/logout/reuse are audited).
- Reuse the existing `MediatR` / `INotification` infrastructure from `cqrs-auth-login`. The three new notifications extend `AuthNotifications.cs`.
- Keep the Application layer free of `HttpContext`, `IPasswordPolicyService`, `IPasswordHasherService`, and `IConfiguration`.

**Non-Goals:**
- Migrating `ChangePassword`, `ForgotPassword`, `ResetPassword`, or `ConfirmEmail` — each is a follow-up change.
- Adding 2FA / refresh-token-binding to device fingerprints.
- Implementing sliding-session refresh tokens (still fixed-TTL 7 days).
- Changing the reuse-detection algorithm (e.g., it does NOT block the user account; it just revokes the user's other refresh tokens — the same behavior as today).
- Adding integration tests with Testcontainers — handler unit tests with Moq are sufficient for the contract preserved by this change.

## Decisions

| # | Decision | Choice | Alternatives considered | Rationale |
|---|----------|--------|-------------------------|-----------|
| 1 | **Result type for refresh outcome** | A new `RefreshResult` value object + `RefreshOutcome(RefreshResult, RefreshResponse?)` returned by the handler. Failure modes: `RefreshErrorCode` enum with `InvalidToken`, `TokenCompromised`, `TokenExpired`, `UserNotFound`. | Throw exceptions for each failure; reuse `LoginResult` with a `LoginErrorCode` field renamed. | Reusing `LoginResult` would tightly couple Login and Refresh — they have different failure sets (Refresh has no `AccountDeactivated` or `EmailNotConfirmed`; Login has no `TokenCompromised`). A separate type is clearer and matches the `cqrs-auth-login` convention. |
| 2 | **Logout result type** | No result type. The handler returns `Unit` and the controller always returns `200 OK`. The handler is allowed to no-op when the token does not exist. | Return a `LogoutResult` with success/failure; throw on missing token. | The current behavior is "always 200". Anything else is a breaking change. Logout is idempotent by design — that is the security property, not a bug to design away. |
| 3 | **Side effects — audit** | Three new `INotification` records: `TokenRefreshedNotification`, `TokenReuseDetectedNotification`, `UserLoggedOutNotification`. Handlers live in `Infrastructure/Notifications/` (one file per notification, each implementing `INotificationHandler<T>`). | Direct calls to `IAuditService` from the command handler. | MediatR notifications are already wired in `cqrs-auth-login`. Direct calls would couple the Application layer to audit timing and ordering. The cost (3 more files) is small. |
| 4 | **`FrontendUrl` plumbing** | Not needed for Refresh or Logout (no emails are sent from these paths). | Pass it through for symmetry. | YAGNI. Add it when ChangePassword/ResetPassword migration needs it. |
| 5 | **Reuse-detection: revoke-all semantics** | Preserved bit-for-bit. The handler calls `IRefreshTokenRepository.RevokeAllForUserAsync(userId, null)` (no `revokedBy`); the current `RevokeAllForUserAsync` signature accepts a nullable `revokedBy` and stores `null` for system-driven revocation. | Track the offending IP in the audit only; do not revoke other sessions. | The current behavior is the spec. Changing it (e.g., soft-revoke only the offending device) is out of scope and would be a security policy change. |
| 6 | **Audit observability for successful refreshes** | Add a `TokenRefreshedNotification` and corresponding audit row. The previous behavior did not audit routine refreshes. | Keep auditing only login/logout/reuse. | Routine refreshes are an audit signal: a sudden spike can indicate token theft. The cost is one extra DB write per refresh, which is negligible (≤1 per 15 min per user). |
| 7 | **Test strategy** | Three layers of tests: <br>• `AuthControllerTests` (existing) — 6 new Refresh tests + 2 new Logout tests prove the controller maps `*Outcome` → `IActionResult`. <br>• `RefreshCommandHandlerTests` (new) — 6 handler tests mirror the controller tests against `Mock<IUnitOfWork>`, `Mock<IJwtService>`, `Mock<IDateTimeProvider>`, `Mock<IMediator>`. <br>• `LogoutCommandHandlerTests` (new) — 2 handler tests. | One mega-test in the controller; integration tests with Testcontainers. | Same pattern as `cqrs-auth-login`: the controller tests are the Regla de Oro baseline; the handler tests are the safety net. Testcontainers are a separate workstream. |
| 8 | **Persistence order in Refresh** | Revoke old token, add new token, single `SaveChangesAsync` at the end. Reuse-detection path: `RevokeAllForUserAsync`, then `SaveChangesAsync`. | Save twice (once for the revoke, once for the add). | Single save = atomic. The current code already uses two saves in the rotation path; the new code uses one. This is a strict improvement, not a behavior change visible from the API. |

```
┌──────────────────────────────────────────────────────────────────────────┐
│                 REFRESH + LOGOUT — TARGET (CQRS)                         │
└──────────────────────────────────────────────────────────────────────────┘

  POST /api/auth/refresh                              POST /api/auth/logout
       │                                                     │
       ▼                                                     ▼
  ┌──────────────────────────────────┐         ┌──────────────────────────────────┐
  │ AuthController.Refresh          │         │ AuthController.Logout           │  (WebApi — thin)
  │   • Builds RefreshCommand       │         │   • Builds LogoutCommand        │
  │   • Sends via IMediator         │         │   • Sends via IMediator         │
  │   • Maps RefreshOutcome → 200/  │         │   • Returns 200 always          │
  │     401 via ApiResponse<T>      │         └────────────────┬─────────────────┘
  └────────────────┬─────────────────┘                          │
                   │ RefreshCommand(                            │ LogoutCommand(
                   │   RefreshToken,                            │   RefreshToken,
                   │   IpAddress, UserAgent)                    │   IpAddress, UserAgent)
                   ▼                                             ▼
  ┌──────────────────────────────────┐         ┌──────────────────────────────────┐
  │ RefreshCommandHandler            │         │ LogoutCommandHandler             │
  │   1. Hash + GetByTokenHashAsync  │         │   1. Hash + GetByTokenHashAsync  │
  │   2. Branch on:                  │         │   2. If found → Revoke()         │
  │      null        → InvalidToken  │         │   3. _mediator.Publish(          │
  │      IsRevoked   → RevokeAllUser │         │        UserLoggedOutNotification)│
  │                    + Compromised │         │   4. Return Unit                 │
  │      IsExpired   → TokenExpired  │         └────────────────┬─────────────────┘
  │      user null/!IsActive         │                          │
  │                  → UserNotFound  │                          ▼
  │   3. Revoke old, generate new    │         ┌──────────────────────────────────┐
  │   4. Persist new RefreshToken    │         │ UserLoggedOutAuditHandler        │
  │   5. _mediator.Publish(          │         │ → IAuditService.LogAsync(         │
  │        TokenRefreshedNotification)│         │     "UserLoggedOut", ...)        │
  │   6. return RefreshOutcome(      │         └──────────────────────────────────┘
  │        Success, RefreshResponse) │
  └────────────────┬─────────────────┘
                   │ _mediator.Publish (fan-out)
                   ▼
  ┌────────────────────┐  ┌────────────────────┐
  │ TokenRefreshed     │  │ TokenReuseDetected │
  │ AuditHandler       │  │ AuditHandler       │
  │ (Infrastructure)   │  │ (Infrastructure)   │
  └────────────────────┘  └────────────────────┘
```

## Risks / Trade-offs

- **[Revoke-then-add is no longer atomic across the reuse-detection path]** → Reuse detection now calls `RevokeAllForUserAsync` + `SaveChangesAsync` in the handler. A crash between the two leaves the user's tokens revoked but no audit row. Mitigation: the audit is published via `INotification` after `SaveChangesAsync` returns. If the publish fails, the worst case is a missing audit log — the user is still secure. Acceptable.
- **[One extra audit row per refresh]** → Every successful refresh now writes a `TokenRefreshed` row. At 1 row per user per 15 min, this is ≤ 96 rows/user/day — negligible. Mitigation: not adding an index or retention policy in this change; revisit if `AuditLog` grows beyond 1M rows.
- **[Pattern spread]** → This change is the second CQRS auth migration in a row. If the pattern is wrong, fixing it now is cheap. After the third migration (`cqrs-auth-password`), the conventions are effectively locked. Mitigation: the `RefreshResult` type intentionally mirrors `LoginResult` so any future change to the pattern affects both.
- **[Logout is fully idempotent]** → A logout for a token that never existed is a 200. An attacker can call logout with any string and get 200 — this is the same as the current behavior. The intent is "the user's session state has the token revoked" — if the token never existed, no work is needed. This is correct; documented for reviewers.

## Migration Plan

1. **Baseline** (Step 1 of `tasks.md`): add 6 Refresh + 2 Logout tests to `AuthControllerTests`. Run `dotnet test` — `48/48` → `56/56` green.
2. **Handler code** (Steps 2–4): create `RefreshResult`, `RefreshCommand`/`Handler`/`Validator`/`Response`/`Outcome`, `LogoutCommand`/`Handler`/`Validator`, 3 new notifications + handlers, handler unit tests. Run `dotnet test` — `64/64` green (8 new handler tests; the 8 controller tests still pass against the *old* code path).
3. **Controller refactor** (Step 5): replace `AuthController.Refresh` and `AuthController.Logout` bodies with MediatR senders. Run `dotnet test` — `64/64` green.
4. **Docs** (Step 6): update `AGENTS.md` to mark Refresh + Logout as 🎯 in the "¿Dónde ocurre la acción?" table.
5. **Rollback** is trivial: revert the diff for those two methods. The new types stay; they will be reused by the next auth migration.

No DB migration, no config change, no docker compose change.

## Open Questions

None.
