## Context

The `AGENTS.md` architecture diagram marks the target state for the backend as a CQRS pipeline where each feature is a vertical slice — Command/Query → Handler → Validator → DTO/Response — with `ValidationBehavior` running in the MediatR pipeline. The current state is "controllers orchestrate" (`AuthController.Login` is 80 lines, depending on 9 collaborators).

The MediatR registration in `AppBaseNetReact.Application/DependencyInjection.cs` is already correct (`AddMediatR` + `AddValidatorsFromAssembly` + `ValidationBehavior` registration). The infrastructure for the underlying behavior is also in place: `IUnitOfWork` with `Users`, `RefreshTokens`, `LoginAttempts` repositories; `IJwtService` for access/refresh token generation; `IPasswordHasherService`; `IPasswordPolicyService`; `IAuditService`; `IEmailService`; `IEmailService` already implements the `AccountLocked` template via `EmailRenderer` (used by the existing `SendAccountLockedEmail`-style flow in `AuthController.SendAccountLockedEmail`).

The Login flow is the entry point that exercises every cross-cutting concern the rest of the auth surface will need: validation, lockout policy, email-confirmation gate, refresh-token persistence, audit logging, and email notification. Getting the conventions right here means `Refresh`, `Logout`, `ChangePassword`, `ForgotPassword`, `ResetPassword`, and `ConfirmEmail` can each be migrated in a follow-up change with the same template.

## Goals / Non-Goals

**Goals:**
- Move all login business logic from `AuthController.Login` into a single `LoginCommandHandler` in the Application layer.
- Keep the HTTP contract (`POST /api/auth/login`) bit-for-bit identical: same response shape, same status codes (`200`, `401`, `403`, `423`), same side effects.
- Establish the cross-cutting notification pattern (MediatR `INotification` + handlers in Infrastructure) for audit logging and outbound email so subsequent auth migrations can reuse it.
- Prove the migration with 8 new tests in `AuthControllerTests` (controller contract) and 8 parallel tests in `LoginCommandHandlerTests` (handler in isolation).
- Preserve the existing rate-limit attribute (`[EnableRateLimiting("Login")]`) and the `[HttpPost("login")]` route — no controller-shape changes.
- Keep the `Application` layer free of `HttpContext`, `EmailRenderer`, `EmailOptions`, and `IConfiguration`.

**Non-Goals:**
- Migrating `Refresh`, `Logout`, `ChangePassword`, `ForgotPassword`, `ResetPassword`, `ConfirmEmail` — each is a follow-up change.
- Adding MFA / 2FA flows.
- Switching JWT algorithm or claim shape.
- Persisting the `LoginAttempt` via a domain event (it remains a direct call to `IUnitOfWork.LoginAttempts.AddAsync` from the handler — moving it to a notification is a separate refactor and adds an at-least-once delivery surface that the current transactional handler does not need).
- Replacing the in-memory `EmailQueueService` with a real queue (Quartz background job is already wired in `email-forgot-password` change and is unaffected here).
- Adding integration tests with Testcontainers — handler unit tests with Moq are sufficient for the contract preserved by this change.

## Decisions

| # | Decision | Choice | Alternatives considered | Rationale |
|---|----------|--------|-------------------------|-----------|
| 1 | **Result type for command outcome** | A new `LoginResult` value object + `LoginOutcome(LoginResult, LoginResponse?)` record returned by the handler. Failure modes are first-class (`LoginErrorCode` enum: `InvalidCredentials`, `AccountDeactivated`, `AccountLocked`, `EmailNotConfirmed`). | Throw exceptions for each failure; return `OneOf<LoginResponse, Failure>`; return `(int status, string message)` tuples. | Exceptions cross the unit-of-work boundary and are easy to swallow in middleware; tuples leak HTTP shape into the Application layer; `OneOf` adds a dependency for one feature. A typed result keeps the handler a pure function of its inputs. |
| 2 | **Mapping `LoginOutcome` → HTTP** | The controller, not the handler, is responsible for converting `LoginErrorCode` into the existing `ApiResponse<T>` + `IActionResult` (401 / 403 / 423). | A custom middleware that maps `LoginErrorCode`; an `IHttpResultMapper` injected into the handler. | The handler stays transport-agnostic. If we add gRPC or a CLI later, the handler does not change. The mapping is small (4 cases) and already implicit in the current controller. |
| 3 | **Side effects — audit + email** | MediatR `INotification` records (`UserLoggedInNotification`, `UserLoginFailedNotification`, `AccountLockedNotification`) published from inside the handler via `_mediator.Publish(...)`. Handlers live in `Infrastructure/Notifications/`. | Direct calls to `IAuditService` / `IEmailService` from the command handler; domain events dispatched by EF Core. | Direct calls couple the Application layer to audit/email timing and ordering. Domain-event-via-EF requires an `IDomainEventDispatcher` registered into `SaveChangesAsync`. MediatR notifications are already wired, are testable with `Mock<IMediator>`, and keep the Application layer free of `IAuditService` / `IEmailService` for this specific concern (the handler still calls `IEmailService` for the account-locked case via the new abstraction — see #4). |
| 4 | **Account-locked email template** | Add `IEmailService.SendAccountLockedEmailAsync(to, userName, lockoutMinutes, resetLink, ct)` to the Application interface. The existing `EmailService` implementation renders the `AccountLocked` template internally — the Application layer never sees `EmailRenderer` or `EmailOptions`. | Pass `IConfiguration` (for `FrontendUrl`) into the handler; pass `EmailOptions` into the handler; render in the controller and call the existing `SendEmailAsync`. | The first leaks `IConfiguration` into Application. The second couples Application to a concrete config shape. The third keeps the email in the WebApi layer, defeating the migration. The chosen option keeps the rendering concern where it belongs (Infrastructure) and the email orchestration in the Application handler. |
| 5 | **`FrontendUrl` plumbing** | The controller reads `IConfiguration["FrontendUrl"]` and passes it on the `LoginCommand` as a `string?` field. Default `"http://localhost:5173"` if not set (matches current behavior). | Inject `IConfiguration` into the handler; add a `FrontendOptions` POCO bound from configuration. | The `FrontendUrl` is a transport-layer concern (it composes the link the user clicks in their email, which the email service then formats). The handler just needs the value; the controller is the only place that resolves HTTP-derived config. |
| 6 | **Password policy inside the handler** | Reuse the existing `IPasswordPolicyService.Validate(password)` call site (it is currently a no-op for the login path because the password is *verified*, not changed — only `MaxFailedAccessAttempts` and `DefaultLockoutMinutes` are read at login time). | Add a new policy check at login. | Adding a policy check at login would reject a user who registered before a policy tightening. The current behavior — verify hash, do not re-validate policy — is correct and is preserved. |
| 7 | **Persistence order** | `LoginAttempt` is persisted inside the same `SaveChangesAsync` call as the `RefreshToken` insert. | Persist `LoginAttempt` in a separate `SaveChangesAsync`; move to a notification. | Single transaction = atomic. Splitting it is pointless for a side-effect log. Notification adds fan-out we do not need. |
| 8 | **`LoginRequestValidator` vs `LoginCommandValidator`** | Replace `LoginRequestValidator` with `LoginCommandValidator` (now wired through MediatR pipeline). | Keep the request validator and add a second one on the command. | Duplication invites drift. The command is what reaches the handler, so its validator is the one that runs in the pipeline. The request DTO lives in the WebApi layer; the Application layer only sees the command. |
| 9 | **Test strategy** | Two layers of tests, both targeting the same 8 scenarios: <br>• `AuthControllerTests` (existing) — verifies that the controller maps `LoginCommand` → `IMediator.Send` → `IActionResult` correctly. <br>• `LoginCommandHandlerTests` (new, in `AppBaseNetReact.Application.Tests`) — verifies the handler in isolation against `Mock<IUnitOfWork>`, `Mock<IJwtService>`, `Mock<IPasswordHasherService>`, `Mock<IPasswordPolicyService>`, `Mock<IMediator>`. | One mega-test in the controller; one mega-test in the handler; integration test with Testcontainers. | The 8 controller tests are the Regla de Oro baseline (they pass against the current implementation; they must continue to pass after the refactor). The 8 handler tests are the new safety net for future changes. Integration tests with Testcontainers are a separate workstream. |

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    LOGIN FLOW — TARGET (CQRS)                            │
└──────────────────────────────────────────────────────────────────────────┘

  POST /api/auth/login
       │
       ▼
  ┌──────────────────────────────────────────┐
  │ AuthController.Login                     │  (WebApi — thin)
  │   • Builds LoginCommand                  │
  │   • Sends via IMediator                  │
  │   • Maps LoginOutcome → IActionResult    │
  └──────────────┬───────────────────────────┘
                 │  LoginCommand(Email, Password, IpAddress, UserAgent, FrontendUrl)
                 ▼
  ┌──────────────────────────────────────────┐
  │ MediatR Pipeline                         │
  │   ┌────────────────────────────────────┐ │
  │   │ ValidationBehavior                │ │  (existing — wired in DI)
  │   │   LoginCommandValidator            │ │
  │   └────────────────────────────────────┘ │
  └──────────────┬───────────────────────────┘
                 ▼
  ┌──────────────────────────────────────────┐
  │ LoginCommandHandler                      │  (Application — orchestrator)
  │   1. IUnitOfWork.Users.GetByEmailAsync  │
  │   2. IPasswordHasherService.Verify      │
  │   3. User.MarkLogin / LockUntil / etc.  │
  │   4. IUnitOfWork.LoginAttempts.AddAsync │
  │   5. IJwtService.GenerateAccessToken    │
  │   6. IUnitOfWork.RefreshTokens.AddAsync │
  │   7. IUnitOfWork.SaveChangesAsync       │
  │   8. _mediator.Publish(                 │
  │        new UserLoggedInNotification(...))│
  │   9. return LoginOutcome(               │
  │        Success, LoginResponse)          │
  └──────────────┬───────────────────────────┘
                 │  _mediator.Publish (fan-out)
                 ▼
  ┌────────────────────┐  ┌─────────────────────────┐  ┌────────────────────┐
  │ UserLoggedIn       │  │ UserLoginFailed         │  │ AccountLocked      │
  │ AuditHandler       │  │ AuditHandler            │  │ EmailHandler       │
  │ (Infrastructure)   │  │ (Infrastructure)        │  │ (Infrastructure)   │
  │ → IAuditService    │  │ → IAuditService         │  │ → IEmailService.   │
  │   .LogAsync(...)   │  │   .LogAsync(...)        │  │   SendAccount...   │
  └────────────────────┘  └─────────────────────────┘  └────────────────────┘
```

## Risks / Trade-offs

- **[Order of Publish vs SaveChanges] Failure to persist after Publish** → `_mediator.Publish` is called *after* `SaveChangesAsync` returns, so a handler that throws cannot leave the audit log out of sync. Mitigation: this is the same ordering used by the current controller (audit is written after persistence).
- **[Notification handler exceptions]** → A failing `AccountLockedEmailHandler` could mask a successful login. Mitigation: `AccountLockedEmailHandler` catches and logs internally (same as the current `try/catch` in `AuthController.SendAccountLockedEmail`'s caller). The audit handler uses `IAuditService.LogAsync` which is awaited — if it throws the original error is rethrown, matching current behavior.
- **[Two SaveChangesAsync calls in the failure paths]** → On invalid-password + lockout-threshold the current code calls `SaveChangesAsync` to persist the failed-count increment, then the audit. The migrated handler does the same. Risk: a handler crash between the two saves leaves a partial state. Mitigation: keep them in a single `SaveChangesAsync` call where possible; this matches the current implementation.
- **[Test suite growth]** → Adding 16 tests (8 controller + 8 handler) for one feature inflates the count quickly. Acceptable: the handler tests are the safety net for future migrations, and the controller tests are the Regla de Oro baseline.
- **[Pattern lock-in]** → Whatever conventions we pick here (notification naming, result-type shape, validator location) will be copied to the next 6 auth migrations. Mitigation: the conventions are documented in `design.md` and re-evaluated in the proposal of each follow-up change.

## Migration Plan

This change is a refactor with **no rollout concerns**:

1. **Baseline** (Step 1 of `tasks.md`): add 8 tests to `AuthControllerTests` covering all Login scenarios. Run `dotnet test` — must show `40/40` green.
2. **Introduce new code** (Steps 2–4): add `IEmailService.SendAccountLockedEmailAsync`, the 3 notification handlers, the `LoginCommand`/`Handler`/`Validator`/`Response` types, and the `LoginCommandHandlerTests`. Run `dotnet test` — must show `48/48` green (the handler tests are independent of the controller and pass against the handler in isolation).
3. **Switch the controller** (Step 5): rewrite `AuthController.Login` to call `IMediator.Send`. Run `dotnet test` — must show `48/48` green. The 8 controller tests prove the HTTP contract is preserved; the 8 handler tests prove the new orchestrator is correct.
4. **Rollback** is trivial: revert the diff for `AuthController.Login`. The new types stay; they will be used by the next auth migration.

No DB migration, no config change, no deploy step beyond a normal `docker compose up --build`.

## Open Questions

None. All decisions are made. The only follow-up (migrating `Refresh`/`Logout`/etc.) is explicitly scoped out and will be its own change.
