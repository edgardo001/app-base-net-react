# auth-forgot-password Specification

## Purpose
TBD - created by archiving change cqrs-auth-password. Update Purpose after archive.
## Requirements
### Requirement: Forgot password always returns the same opaque success message
The system SHALL always return `200 OK` with `Message="If the email exists, a password reset link has been sent."` from `POST /api/auth/forgot-password`, regardless of whether the email matches a registered user, the user is active, or the user has confirmed their email. This is an anti-enumeration property: an attacker MUST NOT be able to distinguish "email registered" from "email not registered" based on the HTTP response.

#### Scenario: Unregistered email returns generic success
- **WHEN** `POST /api/auth/forgot-password` is called with an email that does not match any user
- **THEN** the system SHALL return `200 OK` with `Message="If the email exists, a password reset link has been sent."` and SHALL NOT publish any notification, write any audit row, or send any email

#### Scenario: Registered email returns identical success and queues a reset
- **WHEN** `POST /api/auth/forgot-password` is called with an email that matches a registered user
- **THEN** the system SHALL return `200 OK` with the same `Message="If the email exists, a password reset link has been sent."` and SHALL publish a `PasswordResetRequestedNotification` (which causes an audit row to be persisted and a reset email to be sent)

### Requirement: Forgot password issues a single-use reset token with a 24-hour expiry
When a registered user requests a password reset, the system SHALL generate a 32-byte cryptographic token (hex-encoded), store it in `User.EmailConfirmationToken` with `User.EmailConfirmationTokenExpires = UtcNow + 24 hours`, persist with `SaveChangesAsync`, and include the token in the reset link as `${FrontendUrl}/reset-password?token={token}`.

#### Scenario: Reset token is generated and stored
- **WHEN** a registered email triggers the forgot-password flow
- **THEN** the system SHALL generate a fresh 32-byte token, set `EmailConfirmationToken` and `EmailConfirmationTokenExpires`, persist, and include the token in the reset link sent to the user

### Requirement: Forgot password writes a PasswordResetRequested audit entry
The system SHALL publish a `PasswordResetRequestedNotification` whenever a registered user triggers the flow. The notification SHALL cause an `AuditLog` row to be persisted with `Action="PasswordResetRequested"`, `EntityType="User"`, and `Details="Reset token generated"`.

#### Scenario: Audit row recorded on registered user
- **WHEN** a registered email triggers the forgot-password flow
- **THEN** the system SHALL publish a `PasswordResetRequestedNotification` that causes an `AuditLog` row to be persisted with the user's id, the source IP, the user agent, and `Details="Reset token generated"`

