# auth-reset-password Specification

## Purpose
TBD - created by archiving change cqrs-auth-password. Update Purpose after archive.
## Requirements
### Requirement: Reset password validates the token and rejects unknown or expired tokens
The system SHALL look up the user by `User.EmailConfirmationToken == request.Token`. If no user matches, the system SHALL return `400 Bad Request` with `Message="Invalid reset token"`. If the matched user has `EmailConfirmationTokenExpires < UtcNow`, the system SHALL return `400 Bad Request` with `Message="Reset token has expired"`.

#### Scenario: Unknown token returns 400
- **WHEN** `POST /api/auth/reset-password` is called with a `Token` that does not match any user
- **THEN** the system SHALL return `400 Bad Request` with `Message="Invalid reset token"` and SHALL NOT modify any user or publish any notification

#### Scenario: Expired token returns 400
- **WHEN** `POST /api/auth/reset-password` is called with a `Token` that matches a user whose `EmailConfirmationTokenExpires` is in the past
- **THEN** the system SHALL return `400 Bad Request` with `Message="Reset token has expired"` and SHALL NOT modify the user

### Requirement: Reset password validates the new password against the policy
The system SHALL validate the new password against `IPasswordPolicyService`; failures return `400 Bad Request` with the policy error message and SHALL NOT modify the user.

#### Scenario: Weak new password returns 400
- **WHEN** `POST /api/auth/reset-password` is called with a `NewPassword` that fails `IPasswordPolicyService.Validate`
- **THEN** the system SHALL return `400 Bad Request` with the policy error message and SHALL NOT modify the user or publish any notification

### Requirement: Reset password atomically updates the user and forces a password change on next login
The system SHALL call `User.SetPasswordHash(newHash)`, `User.ForcePasswordChange()`, and `User.ConfirmEmail()` as a single unit of work, persist with a single `SaveChangesAsync`, and return `200 OK` with `Message="Password reset successfully"`.

#### Scenario: Valid reset persists the new password and forces change on next login
- **WHEN** `POST /api/auth/reset-password` is called with a valid non-expired token and a `NewPassword` that passes the policy
- **THEN** the system SHALL update the password hash, force a password change on next login, mark the email as confirmed, persist with `SaveChangesAsync`, and return `200 OK` with `Message="Password reset successfully"`

### Requirement: Reset password writes a PasswordReset audit entry and sends a notification email
The system SHALL publish a `PasswordResetNotification` after `SaveChangesAsync` succeeds. The notification SHALL cause an `AuditLog` row to be persisted with `Action="PasswordReset"`, `EntityType="User"`, and `Details="Password reset via token"`, AND SHALL cause a confirmation email to be sent via `IEmailService.SendPasswordChangedEmailAsync` (same template as the change-password flow).

#### Scenario: Successful reset records audit and sends email
- **WHEN** the password reset completes successfully
- **THEN** the system SHALL publish a `PasswordResetNotification` that causes an `AuditLog` row to be persisted with the user's id, the source IP, the user agent, and `Details="Password reset via token"`, AND a `SendPasswordChangedEmailHandler` SHALL call `IEmailService.SendPasswordChangedEmailAsync`

