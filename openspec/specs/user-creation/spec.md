# user-creation Specification

## Purpose
TBD - created by archiving change user-creation-email-confirmation. Update Purpose after archive.
## Requirements
### Requirement: Create User Persists With Unconfirmed Email
The system SHALL create a new user when an admin posts to `POST /api/users` with a valid `CreateUserRequest`, persist the user with `EmailConfirmed = false`, assign any provided `RoleIds` to the new user, and return HTTP 201 with an `ApiResponse<object>` containing the new user's `id` and `email`.

#### Scenario: Valid request creates the user
- **WHEN** an admin posts a `CreateUserRequest` with a unique email, first name, last name, password, and optional `RoleIds`
- **THEN** the system MUST hash the password, create the `User` via `User.Create(...)`, add any requested `UserRole` associations, persist via `IUnitOfWork.Users.AddAsync` + `IUnitOfWork.SaveChangesAsync`, and return HTTP 201 with `ApiResponse<object>.Ok(new { id, email })`

#### Scenario: Created user is not yet email-confirmed
- **WHEN** the user is persisted
- **THEN** the persisted `User.EmailConfirmed` MUST be `false`

### Requirement: Duplicate Email Is Rejected
The system SHALL reject a `POST /api/users` request whose email already corresponds to an existing user by returning HTTP 409 with `ApiResponse<object>.Fail` containing message `"Email already registered"` and MUST NOT create a new user, MUST NOT generate a token, and MUST NOT send any email.

#### Scenario: Existing email returns 409
- **WHEN** `IUserRepository.GetByEmailAsync(request.Email)` returns a non-null user
- **THEN** the controller MUST return HTTP 409 with `ApiResponse<object>.Fail("Email already registered")` and MUST NOT call `IUserRepository.AddAsync`, `IUnitOfWork.SaveChangesAsync`, or `IEmailService.SendEmailAsync`

### Requirement: Confirmation Token Is Generated And Stored
The system SHALL generate a cryptographically random 64-character hex confirmation token, store it on the new user via `User.SetEmailConfirmationToken(token, expiry)`, and set the expiry to `DateTime.UtcNow + 24 hours` so that the existing `POST /api/auth/confirm-email` flow can later validate it.

#### Scenario: Token is set on the created user
- **WHEN** the user is persisted
- **THEN** `User.EmailConfirmationToken` MUST be a non-null, non-empty 64-character hex string and `User.EmailConfirmationTokenExpires` MUST be within 1 minute of `DateTime.UtcNow + 24 hours`

#### Scenario: Token generation uses a cryptographic RNG
- **WHEN** the token is generated
- **THEN** the controller MUST use `System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)` and `Convert.ToHexString` so the token is uniformly distributed and unpredictable

### Requirement: EmailConfirmation Email Is Sent On Creation
The system SHALL send an `EmailConfirmation` email to the new user containing a confirmation link of the form `${Request.Scheme}://${Request.Host}/confirm-email?token={token}` so that the user can complete the email confirmation flow.

#### Scenario: EmailConfirmation email is dispatched
- **WHEN** the user has been persisted
- **THEN** the controller MUST call `IEmailService.SendEmailAsync` exactly once with the user's email, the `EmailConfirmation` template subject, and a body that contains the `ConfirmationLink` variable whose value is `${Request.Scheme}://${Request.Host}/confirm-email?token={token}`

#### Scenario: Welcome email is NOT sent on creation
- **WHEN** the user is created
- **THEN** the controller MUST NOT use the `Welcome` template — the welcome email is dispatched only after the user successfully confirms their email (see the `auth-confirm-email` capability)

### Requirement: Existing Auth Confirm Flow Closes The Loop
The system SHALL rely on the existing `POST /api/auth/confirm-email` endpoint (see `auth-confirm-email` capability) to validate the token, mark `EmailConfirmed = true`, persist the change, and dispatch the `Welcome` email — no additional confirmation logic is added by this capability.

#### Scenario: Confirmation completes the flow
- **WHEN** the user clicks the confirmation link and the frontend posts the token to `POST /api/auth/confirm-email`
- **THEN** the `auth-confirm-email` flow takes over and the user becomes `EmailConfirmed = true`; the `Welcome` email is then sent automatically by `EmailConfirmedEmailHandler`

### Requirement: Confirmation Link Points To Configured Frontend URL
The system SHALL compose the email confirmation link as `{EmailOptions.FrontendBaseUrl.TrimEnd('/')}/confirm-email?token={token}` so that the link lands on the frontend SPA, not on the API server.

#### Scenario: Default frontend URL is used in dev
- **WHEN** `POST /api/users` is invoked and `Email:FrontendBaseUrl` is not explicitly configured (e.g. local dev with the bundled `appsettings.json`)
- **THEN** the email body MUST contain the link `http://localhost:5173/confirm-email?token={token}` and MUST NOT contain the API host (`Request.Host`)

#### Scenario: Configured frontend URL is honoured in any environment
- **WHEN** `Email:FrontendBaseUrl` is set to any value (e.g. `https://app.example.com`)
- **THEN** the email body MUST contain the link `https://app.example.com/confirm-email?token={token}` regardless of the request's `Host` header or scheme, and MUST NOT contain `Request.Host` or `Request.Scheme`

#### Scenario: Trailing slash in the configured URL is normalised
- **WHEN** `Email:FrontendBaseUrl` is configured with a trailing `/` (e.g. `https://app.example.com/`)
- **THEN** the rendered link MUST be `https://app.example.com/confirm-email?token={token}` (no double slash)

### Requirement: Frontend Provides A Public Confirm-Email Page
The system SHALL expose a public route at `/confirm-email` in the SPA that, when opened with a `?token={token}` query string, POSTs the token to the backend's `POST /api/auth/confirm-email` endpoint and displays the outcome to the user.

#### Scenario: Valid token confirms the email
- **WHEN** the user opens `https://{frontend}/confirm-email?token={validToken}`
- **THEN** the page MUST POST `{ token }` to `/api/auth/confirm-email`, display a "Correo confirmado" success state with a link to `/login`, and the backend MUST mark the user as `EmailConfirmed`

#### Scenario: Missing token shows the error state without an API call
- **WHEN** the user opens `https://{frontend}/confirm-email` with no `?token=` query string
- **THEN** the page MUST render the error state ("Enlace inválido") and MUST NOT POST to the backend

#### Scenario: Invalid or expired token shows the error state with the backend message
- **WHEN** the user opens `https://{frontend}/confirm-email?token={invalidOrExpiredToken}`
- **THEN** the page MUST render the error state with the backend's error message, MUST NOT crash, and MUST provide a link back to `/login`

#### Scenario: Route is public
- **WHEN** the SPA route table is built
- **THEN** `/confirm-email` MUST be registered outside any authentication guard (the user is not yet logged in at this point)

### Requirement: Create User Without Admin-Supplied Password
The system SHALL create a new user when an admin posts to `POST /api/users` with a `CreateUserRequest` containing only `email`, `firstName`, `lastName`, and optional `roleIds` — the request SHALL NOT include a `password` field. The system SHALL generate a cryptographically random 12-character password using `IRandomPasswordGenerator` and hash it via `IPasswordHasherService` before persisting. The user SHALL be persisted with `LastPasswordChangeAt = null` via `user.ForcePasswordChange()` so the next login forces a password change.

#### Scenario: Admin creates a user with no password in the request
- **WHEN** an admin posts a `CreateUserRequest` with `(email, firstName, lastName, roleIds?)` and no `password` field
- **THEN** the system MUST generate a 12-character password, hash it, create the `User`, call `ForcePasswordChange()`, persist, and return HTTP 201

#### Scenario: Request containing a password field is rejected
- **WHEN** an admin posts a `CreateUserRequest` that includes a `password` field
- **THEN** FluentValidation MUST reject the request with HTTP 400 because the DTO has no `Password` property to bind to (extra fields in the JSON body are ignored, but the missing `Password` is not a validation issue — the test for the new DTO shape is simply that the request succeeds without it)

#### Scenario: Generated password satisfies policy
- **WHEN** the password is generated
- **THEN** it MUST be exactly 12 characters long, MUST contain at least one uppercase letter, one lowercase letter, and one digit, and MUST be sampled from `System.Security.Cryptography.RandomNumberGenerator` (not `System.Random`)

### Requirement: EmailConfirmation Email Includes Temporary Password
The system SHALL send an `EmailConfirmation` email whose body contains the auto-generated plain-text password via a `{{TemporaryPassword}}` variable so the user can log in with it. The template SHALL render the password in a clearly styled block with copy in Spanish explaining that the user must change the password on first login.

#### Scenario: Initial onboarding email contains the temporary password
- **WHEN** the user is created by `CreateUser`
- **THEN** the `EmailConfirmation` email body MUST contain the same plain-text password that was hashed and stored in the database, and MUST render it via the `{{TemporaryPassword}}` template variable (replacing the existing `{{ConfirmationLink}}` block as before)

#### Scenario: Email template requires the variable
- **WHEN** the email is rendered
- **THEN** the `EmailRenderer` MUST throw `InvalidOperationException` if `{{TemporaryPassword}}` is not provided (existing strict-variable behaviour)

### Requirement: Resend Onboarding Email Endpoint
The system SHALL provide a `POST /api/users/{id}/resend-onboarding-email` endpoint that allows an admin to re-trigger the onboarding email for a user who has not yet confirmed their email. The endpoint SHALL regenerate the confirmation token (invalidating any previously sent link), persist the change, and send a new `EmailConfirmation` email containing the new confirmation link.

#### Scenario: Resend for an unconfirmed user
- **WHEN** an admin posts to `/api/users/{id}/resend-onboarding-email` for a user with `EmailConfirmed = false`
- **THEN** the system MUST generate a fresh `EmailConfirmationToken` (32-byte hex, 24h expiry), persist via `IUnitOfWork.SaveChangesAsync`, send the `EmailConfirmation` email, publish an `OnboardingEmailResentNotification`, and return HTTP 200 with an `ApiResponse<object>` containing a success message

#### Scenario: Resend invalidates the previous token
- **WHEN** a resend succeeds
- **THEN** any prior confirmation link in the user's inbox MUST no longer resolve (the previous `EmailConfirmationToken` has been overwritten). The new email is the only way to confirm

#### Scenario: Resend for an already-confirmed user is rejected
- **WHEN** an admin posts to `/api/users/{id}/resend-onboarding-email` for a user with `EmailConfirmed = true`
- **THEN** the system MUST return HTTP 409 with `ApiResponse<object>.Fail` containing a message explaining that the user has already confirmed (there is nothing to resend) and MUST NOT send a new email, MUST NOT regenerate the token, and MUST NOT publish the notification

#### Scenario: Resend for an unknown user is rejected
- **WHEN** an admin posts to `/api/users/{id}/resend-onboarding-email` for a user that does not exist
- **THEN** the system MUST return HTTP 404 with `ApiResponse<object>.Fail` and MUST NOT send any email

#### Scenario: Resend is audited
- **WHEN** a resend succeeds
- **THEN** the `OnboardingEmailResentAuditLogHandler` MUST write an `OnboardingEmailResent` audit log entry including `userId`, `ipAddress` from the request, and `userAgent` from the request

