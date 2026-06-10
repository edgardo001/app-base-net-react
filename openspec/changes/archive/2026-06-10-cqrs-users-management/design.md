## Context

The User Management Platform uses hexagonal architecture with CQRS via MediatR. The Auth module (7 commands, 8 handlers, 11 notifications) is fully migrated and serves as the reference implementation. The Users module has 12 controller actions with inline business logic, except `ResendOnboardingEmail` which is already migrated.

**Current state:**
- `UsersController` injects `IUnitOfWork`, `IPasswordHasherService`, `IRandomPasswordGenerator`, `IEmailService`, `IFileStorageService`, `IAuditService`, `IEmailRenderer`, `IOptions<EmailOptions>`, `IOptions<StorageOptions>`
- Business logic includes: duplicate email checks, password generation, entity creation, role assignment, email confirmation, file validation, audit logging
- DTOs (`UserDto`, `UserDetailDto`, `RoleDto`) are defined inline in `UsersController.cs`
- Existing 26 controller tests serve as API contract tests

## Goals / Non-Goals

**Goals:**
- Migrate all 11 remaining Users actions to MediatR command/query handlers
- Establish consistent CQRS pattern across all domains (Auth + Users)
- Maintain API contract compatibility (same HTTP status codes, response shapes)
- Create handler-level unit tests before implementation (Regla de Oro)
- Create notification handlers for audit logging (consistent with Auth pattern)

**Non-Goals:**
- Modify API endpoints or response shapes
- Add new endpoints
- Migrate Roles, Profile, or Admin modules (future changes)
- Implement CQRS queries for read-only endpoints that could use simpler patterns
- Change the existing `IUnitOfWork` pattern

## Decisions

### D1: Per-Feature Folder Structure

**Decision:** Each command/query in its own folder with Command, Handler, Validator, Outcome/Response files.

**Rationale:** Matches the Auth pattern (`Login/`, `Refresh/`, `Logout/`). Each folder is self-contained, easy to find, test, and delete.

**Alternatives considered:**
- Grouped by type (`Commands/`, `Queries/` with flat files): harder to find related files
- Single handler file per domain: violates single-responsibility

### D2: Outcome Types for Commands

**Decision:** Each command returns an Outcome type (e.g., `CreateUserOutcome`, `UpdateUserOutcome`) with discriminated results.

**Rationale:** Auth uses `LoginOutcome`, `RefreshOutcome`, etc. with `Success`, `UserNotFound`, `InvalidCredentials`, etc. This pattern provides structured error handling without exceptions.

**Alternatives considered:**
- Return `ApiResponse<T>` directly from handlers: couples handlers to HTTP concerns
- Use exceptions for control flow: harder to test, less explicit

### D3: Notification Handlers for Audit

**Decision:** Create notification handlers that call `IAuditService.LogAsync` (same as Auth pattern).

**Rationale:** The Auth module has 11 notification handlers for audit + email. This pattern decouples audit logic from business logic, making handlers easier to test.

**Alternatives considered:**
- Direct `IAuditService` calls in handlers: couples audit to business logic, harder to test
- Pipeline behavior for audit: loses semantic context (which entity, what changed)

### D4: DTOs Inline in Response Types

**Decision:** DTOs defined in query/command response files (e.g., `GetUserResponse.cs` contains `UserDetailDto`).

**Rationale:** Auth uses `LoginResponse`, `RefreshResponse` with inline DTOs. Feature-scoped DTOs prevent unintended coupling.

**Alternatives considered:**
- Shared `Application/Common/DTOs/`: risk of DTOs being used in unintended contexts
- Separate DTO files per feature: adds file count without proportional benefit

### D5: Implementation Order

**Decision:** Queries → Simple Commands → Complex Commands → File Commands.

**Rationale:** Each phase builds on the previous. Queries establish DTO conventions. Simple commands validate the Outcome pattern. Complex commands depend on those conventions. Avatar is last because file serving (`PhysicalFileResult`) is an HTTP concern.

**Order:**
1. `GetUsers`, `GetUser` (queries)
2. `ToggleActive`, `RevokeTokens` (simple commands)
3. `UpdateUser`, `DeleteUser` (moderate commands)
4. `AdminResetPassword` (email-dependent)
5. `CreateUser` (most complex)
6. `UploadAvatar`, `GetAvatar` (file I/O)

### D6: GetAvatar Handler Design

**Decision:** Handler returns `(string FilePath, string ContentType)` tuple, controller maps to `PhysicalFileResult`.

**Rationale:** `PhysicalFileResult` is an HTTP concern that doesn't belong in Application layer. Handler returns data, controller handles HTTP-specific response.

**Alternatives considered:**
- Keep `GetAvatar` in controller: breaks CQRS consistency
- Handler returns `IFormFile`: couples to HTTP abstraction

### D7: Validation via FluentValidation

**Decision:** Each command/query has a FluentValidation validator registered in DI, executed by `ValidationBehavior` pipeline.

**Rationale:** Consistent with Auth pattern. Validators are co-located with their commands. The existing `ValidationBehavior` pipeline handles automatic validation before handlers execute.

**Alternatives considered:**
- Manual validation in handlers: duplicates validation logic, harder to test
- Data annotations: less expressive, can't express cross-field rules

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Breaking API contracts | Existing 26 controller tests serve as contract tests. Run `dotnet test` after each phase. |
| Missing business logic | Each handler test must verify domain method calls (`ForcePasswordChange`, `SetEmailConfirmationToken`, etc.) |
| Email sending pattern inconsistency | Auth uses notification handlers; CreateUser currently calls `IEmailService` directly. Migrate to notification pattern. |
| GetAvatar HTTP concern | Handler returns tuple, controller maps to `PhysicalFileResult`. Minimal HTTP leakage. |
| DI registration complexity | Auto-registration via `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` handles most cases. Notification handlers in Infrastructure need explicit registration. |
