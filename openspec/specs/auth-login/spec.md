# auth-login Specification

## Purpose
TBD - created by archiving change secure-user-onboarding. Update Purpose after archive.
## Requirements
### Requirement: Login Response Surfaces Password Change Required
The system SHALL set the `PasswordExpired` field on the `LoginResponse` to `true` when `User.IsPasswordExpired()` returns `true` (which is the case when `LastPasswordChangeAt == null` — i.e. the user has never set their own password — OR when the password is older than `PasswordExpirationDays`).

#### Scenario: First login with auto-generated password
- **WHEN** a user logs in for the first time with the auto-generated password sent in the `EmailConfirmation` email
- **THEN** the `LoginResponse.PasswordExpired` MUST be `true` (because `User.ForcePasswordChange()` was called at creation time, leaving `LastPasswordChangeAt == null`)

#### Scenario: Login after the user changes their password
- **WHEN** a user logs in after submitting `/change-password` (which calls `user.SetPasswordHash(...)` setting `LastPasswordChangeAt = DateTime.UtcNow`)
- **THEN** the `LoginResponse.PasswordExpired` MUST be `false`

#### Scenario: Login after the 30-day password expiration window
- **WHEN** a user logs in more than 30 days after their last password change
- **THEN** the `LoginResponse.PasswordExpired` MUST be `true` and the user MUST be forced to change their password

