## ADDED Requirements

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
