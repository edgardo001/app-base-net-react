# admin-test-email Specification

## Purpose
The system provides an admin-facing capability to send a test email through the configured SMTP provider, so that operators can verify the email pipeline end-to-end (template rendering, SMTP host, credentials, retry logic) without waiting for a real production event (user creation, password recovery, etc.). The endpoint is `POST /api/admin/test-email`; the body is `{ to: string }`; the response is a standard `ApiResponse<TestEmailResponse>`. Authorization is asymmetric on `AdminController`: the test-email action accepts `SuperAdmin` OR `Admin`, while the dashboard, audit log, and token revocation actions remain `SuperAdmin` only.
## Requirements
### Requirement: Admin can send a test email
The system SHALL allow users with the `Admin` OR `SuperAdmin` role to send a test email to verify SMTP configuration.

#### Scenario: SuperAdmin sends a test email
- **WHEN** a user with the `SuperAdmin` role enters a valid email address and clicks "Enviar Correo de Prueba"
- **THEN** the system sends an email using the configured SMTP provider
- **AND** the system returns a success response
- **AND** the frontend shows a success toast

#### Scenario: Admin sends a test email
- **WHEN** a user with the `Admin` role (and not `SuperAdmin`) enters a valid email address and clicks "Enviar Correo de Prueba"
- **THEN** the system sends an email using the configured SMTP provider
- **AND** the system returns a success response
- **AND** the frontend shows a success toast

#### Scenario: Invalid email address
- **WHEN** a user with the `Admin` or `SuperAdmin` role enters an invalid email address
- **THEN** the system returns a validation error
- **AND** the frontend shows an error toast with the validation message

#### Scenario: SMTP not configured
- **WHEN** SMTP host is not configured
- **THEN** the system returns an error response
- **AND** the frontend shows an error toast

#### Scenario: Audit logging
- **WHEN** a test email is sent
- **THEN** the system SHALL log the action "TestEmailSent" in the audit log with the recipient email
- **AND** the audit log entry MUST record the actor's user id and the role used to authorize the action

#### Scenario: User without Admin or SuperAdmin role is rejected
- **WHEN** an authenticated user with neither the `Admin` nor the `SuperAdmin` role attempts to send a test email
- **THEN** the system MUST return HTTP 403 Forbidden
- **AND** the frontend MUST NOT render the test-email form (the form is gated on the JWT carrying one of those role claims)

### Requirement: AdminController role asymmetry
The `AdminController` MUST apply authorization asymmetrically across its actions. The class-level rule restricts the controller to the `SuperAdmin` role, but the `SendTestEmail` action MUST be opened up at the action level to additionally accept the `Admin` role. All other actions on the controller (dashboard, audit log, token revocation) MUST remain `SuperAdmin` only.

#### Scenario: SendTestEmail accepts SuperAdmin
- **WHEN** a user with the `SuperAdmin` role calls `POST /api/admin/test-email`
- **THEN** the system authorizes the request based on the class-level `SuperAdmin` rule (or the action-level override, which is a superset)

#### Scenario: SendTestEmail accepts Admin
- **WHEN** a user with the `Admin` role (and not `SuperAdmin`) calls `POST /api/admin/test-email`
- **THEN** the system authorizes the request based on the action-level `[Authorize(Roles = "SuperAdmin,Admin")]` override

#### Scenario: Other AdminController actions reject Admin
- **WHEN** a user with the `Admin` role (and not `SuperAdmin`) calls `GET /api/admin/dashboard`, `GET /api/admin/audit-log`, or `POST /api/admin/revoke-all-tokens`
- **THEN** the system MUST return HTTP 403 Forbidden (no action-level override on those endpoints)

#### Scenario: Asymmetry is preserved by regression test
- **WHEN** a future developer reverts the action-level override on `SendTestEmail`
- **THEN** the regression test `SendTestEmail_AuthorizeAttribute_AcceptsAdminAndSuperAdmin` in `AdminControllerTests` MUST fail (reflection-based assertion that the action's `[Authorize]` attribute contains exactly the `SuperAdmin` and `Admin` roles)

### Requirement: Test-email authorization depends on JWT role claims
The test-email endpoint is authorized by the `[Authorize(Roles = "...")]` attribute, which evaluates the role claims in the JWT. For the role check to be meaningful, the JWT issued at login MUST include the user's current role and permission claims. This is the responsibility of `LoginCommandHandler` and the `GetByEmailAsync` repository method it relies on.

#### Scenario: GetByEmailAsync deep-loads UserRoles navigation
- **WHEN** the `LoginCommandHandler` calls `IUnitOfWork.Users.GetByEmailAsync(email)` to load the user for JWT generation
- **THEN** the returned `User` MUST have its `UserRoles` navigation populated
- **AND** each `UserRole` MUST have its `Role` navigation populated
- **AND** each `Role` MUST have its `RolePermissions` navigation populated
- **AND** each `RolePermission` MUST have its `Permission` navigation populated
- **BECAUSE** the JWT writer (`JwtService.GenerateAccessToken`) iterates `user.UserRoles` to emit role claims and `user.UserRoles.SelectMany(ur => ur.Role.RolePermissions)` to emit permission claims; an empty navigation collection yields a JWT with zero role/permission claims, which makes every `[Authorize(Roles = "...")]` endpoint reject every user

#### Scenario: Regression test guards the Include
- **WHEN** a future developer removes the `Include` chain from `GetByEmailAsync`
- **THEN** the regression test `GetByEmailAsync_ReturnsUserWithRolesAndPermissionsLoaded` in `UserConfirmationTokenPersistenceTests` MUST fail (asserts the navigation properties are populated after a `ChangeTracker.Clear()` and a fresh load)

#### Scenario: Documented contract for downstream callers
- **WHEN** a future developer adds a new caller of `GetByEmailAsync` (forgot-password, reset-password, change-password, confirm-email, etc.)
- **THEN** the caller MUST treat the returned `User` as having all navigations loaded
- **AND** the caller MUST NOT call `Include` again (would be a no-op with tracking on, or a perf hit with `AsNoTracking`)

