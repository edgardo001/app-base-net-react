## ADDED Requirements

### Requirement: Change password requires authentication and a valid current password
The system SHALL require a valid JWT (with a `sub` claim resolving to a real `User`) for `POST /api/auth/change-password`. The system SHALL reject the request with `401 Unauthorized` if the `sub` claim is missing or does not parse as a `Guid`, and with `404 Not Found` if the referenced user does not exist. The system SHALL verify that `CurrentPassword` matches the stored password hash using `IPasswordHasherService`; mismatches return `400 Bad Request` with `Message="Current password is incorrect"`.

#### Scenario: Missing sub claim returns 401
- **WHEN** `POST /api/auth/change-password` is called without a valid JWT
- **THEN** the system SHALL return `401 Unauthorized` and SHALL NOT invoke the change-password command handler

#### Scenario: User not found returns 404
- **WHEN** `POST /api/auth/change-password` is called with a `sub` claim whose value does not match any user in the database
- **THEN** the system SHALL return `404 Not Found`

#### Scenario: Wrong current password returns 400
- **WHEN** `POST /api/auth/change-password` is called with a `CurrentPassword` that does not match the stored hash
- **THEN** the system SHALL return `400 Bad Request` with `Message="Current password is incorrect"` and SHALL NOT modify the user

### Requirement: Change password validates the new password against the policy
The system SHALL validate the new password against `IPasswordPolicyService`; failures return `400 Bad Request` with the policy error message and SHALL NOT modify the user.

#### Scenario: Weak new password returns 400
- **WHEN** `POST /api/auth/change-password` is called with a `NewPassword` that fails `IPasswordPolicyService.Validate`
- **THEN** the system SHALL return `400 Bad Request` with the policy error message and SHALL NOT modify the user or publish any notification

### Requirement: Change password atomically rotates the password and revokes all sessions
The system SHALL update the user's password hash via `User.SetPasswordHash`, revoke all of the user's refresh tokens via `IRefreshTokenRepository.RevokeAllForUserAsync(userId, userId)`, persist with a single `SaveChangesAsync`, and return `200 OK` with `Message="Password changed successfully"`.

#### Scenario: Valid change persists new hash and revokes all tokens
- **WHEN** `POST /api/auth/change-password` is called with the correct `CurrentPassword` and a `NewPassword` that passes the policy
- **THEN** the system SHALL update the password hash, call `IRefreshTokenRepository.RevokeAllForUserAsync(userId, userId)`, persist with `SaveChangesAsync`, and return `200 OK` with `Message="Password changed successfully"`

### Requirement: Change password writes a PasswordChanged audit entry and sends a notification email
The system SHALL publish a `PasswordChangedNotification` after `SaveChangesAsync` succeeds. The notification SHALL cause an `AuditLog` row to be persisted with `Action="PasswordChanged"` and `EntityType="User"`, AND SHALL cause a confirmation email to be sent to the user's registered email address via `IEmailService.SendPasswordChangedEmailAsync`. A failure to send the email SHALL be logged but SHALL NOT change the HTTP response (still `200 OK`).

#### Scenario: Successful change records audit and sends email
- **WHEN** the change-password rotation completes successfully
- **THEN** the system SHALL publish a `PasswordChangedNotification` that causes an `AuditLog` row to be persisted with the user's id, the source IP, and the user agent, AND a `SendPasswordChangedEmailHandler` SHALL call `IEmailService.SendPasswordChangedEmailAsync`

#### Scenario: Email send failure does not change the response
- **WHEN** the email-sending handler throws
- **THEN** the handler SHALL swallow the exception, log it via `ILogger<T>`, and the HTTP response SHALL remain `200 OK` with `Message="Password changed successfully"`
