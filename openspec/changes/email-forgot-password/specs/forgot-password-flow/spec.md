## ADDED Requirements

### Requirement: Forgot Password generates token and emails reset link
The system SHALL generate a reset token, store it hashed in the User entity, and email a reset link — never return the token in the API response.

#### Scenario: Valid email sends reset email
- **WHEN** `POST /api/auth/forgot-password` is called with a registered email
- **THEN** the system SHALL generate a 32-char reset token, store it in `EmailConfirmationToken` with 24h expiry, send an email with the reset link, and return `200 OK` with a generic success message

#### Scenario: Unregistered email returns generic success
- **WHEN** `POST /api/auth/forgot-password` is called with an unregistered email
- **THEN** the system SHALL return `200 OK` with the same generic message to prevent email enumeration

#### Scenario: Reset link contains single-use token
- **WHEN** the user clicks the reset link
- **THEN** the frontend SHALL navigate to `/reset-password?token=<token>` and allow the user to enter a new password

### Requirement: Reset Password validates token and changes password
The system SHALL validate the reset token, apply password policy, and change the password.

#### Scenario: Valid token and password
- **WHEN** `POST /api/auth/reset-password` is called with a valid token and a new password meeting policy
- **THEN** the system SHALL hash and store the new password, clear the reset token, force password change on next login, send "password-changed" email, and return `200 OK`

#### Scenario: Expired token
- **WHEN** `POST /api/auth/reset-password` is called with an expired token (older than 24h)
- **THEN** the system SHALL return `400 Bad Request` with "Reset token has expired"

#### Scenario: Invalid token
- **WHEN** `POST /api/auth/reset-password` is called with a token that does not match any user
- **THEN** the system SHALL return `400 Bad Request` with "Invalid reset token"

#### Scenario: Password does not meet policy
- **WHEN** `POST /api/auth/reset-password` is called with a password that violates the password policy
- **THEN** the system SHALL return `400 Bad Request` with the policy validation error

### Requirement: Change Password sends email notification
The system SHALL send a "password-changed" email when a user changes their password via the authenticated endpoint.

#### Scenario: Password changed sends notification
- **WHEN** `POST /api/auth/change-password` succeeds
- **THEN** the system SHALL send a "password-changed" email to the user's email address

### Requirement: Account lock sends email notification
The system SHALL send an "account-locked" email when a user's account is locked due to failed login attempts.

#### Scenario: Account locked sends notification
- **WHEN** a login attempt fails and the user's account becomes locked (AccessFailedCount >= MaxFailedAccessAttempts)
- **THEN** the system SHALL send an "account-locked" email to the user's email address
