## ADDED Requirements

### Requirement: Login endpoint authenticates a user with email and password
The system SHALL authenticate a user via `POST /api/auth/login` using email and password, returning a JWT access token and refresh token on success.

#### Scenario: Valid credentials return tokens
- **WHEN** `POST /api/auth/login` is called with a registered email and the correct password
- **THEN** the system SHALL return `200 OK` with `{ AccessToken, RefreshToken, ExpiresAt, User, Permissions, PasswordExpired }`

#### Scenario: Unknown email returns generic unauthorized
- **WHEN** `POST /api/auth/login` is called with an email that does not exist
- **THEN** the system SHALL return `401 Unauthorized` with `{ Success: false, Message: "Invalid email or password" }` and SHALL NOT reveal whether the email is registered

#### Scenario: Invalid password returns generic unauthorized and increments the failure counter
- **WHEN** `POST /api/auth/login` is called with a registered email and an incorrect password
- **THEN** the system SHALL return `401 Unauthorized` with `{ Success: false, Message: "Invalid email or password" }` AND the user's `AccessFailedCount` SHALL be incremented by 1 AND a `LoginAttempt` row SHALL be persisted with `Success=false, FailureReason="Invalid credentials"`

#### Scenario: Deactivated account returns unauthorized
- **WHEN** `POST /api/auth/login` is called with valid credentials for a user whose `IsActive` is `false`
- **THEN** the system SHALL return `401 Unauthorized` with `{ Success: false, Message: "Account is deactivated" }` AND a `LoginAttempt` row SHALL be persisted with `FailureReason="Account deactivated"`

### Requirement: Account lockout after repeated failed attempts
The system SHALL lock the user account and notify the user when the number of consecutive failed access attempts reaches the configured threshold.

#### Scenario: Lockout threshold triggers account lock
- **WHEN** `POST /api/auth/login` is called with invalid credentials and the user's `AccessFailedCount` reaches `IPasswordPolicyService.MaxFailedAccessAttempts` (default 5)
- **THEN** the system SHALL set `LockoutEnd = UtcNow + DefaultLockoutMinutes` (default 15), persist a `LoginAttempt` row with `FailureReason="Invalid credentials"`, publish an `AccountLockedNotification`, and the account-locked email SHALL be sent to the user

#### Scenario: Locked account returns 423 Locked
- **WHEN** `POST /api/auth/login` is called with valid credentials for a user whose `LockoutEnd` is in the future
- **THEN** the system SHALL return `423 Locked` with the remaining lockout minutes in the message AND a `LoginAttempt` row SHALL be persisted with `FailureReason="Account locked"`

### Requirement: Unconfirmed email blocks login
The system SHALL prevent login for users who have not confirmed their email.

#### Scenario: Unconfirmed email returns 403
- **WHEN** `POST /api/auth/login` is called with valid credentials for a user whose `EmailConfirmed` is `false`
- **THEN** the system SHALL return `403 Forbidden` with `{ Success: false, Message: "Email not confirmed. Check your inbox." }` AND a `LoginAttempt` row SHALL be persisted with `FailureReason="Email not confirmed"`

### Requirement: Successful login issues tokens, persists a refresh token, and writes an audit entry
The system SHALL issue a new JWT access token and refresh token, persist a hashed refresh token, and write a `UserLoggedIn` audit log entry on every successful login.

#### Scenario: Successful login marks the user and emits side effects
- **WHEN** `POST /api/auth/login` succeeds
- **THEN** the system SHALL call `user.MarkLogin()` (sets `LastLoginAt = UtcNow`, resets `AccessFailedCount`), generate an access token with the user's permission codes, generate a refresh token, persist a new `RefreshToken` row with `ExpiresAt = UtcNow + 7 days`, and publish a `UserLoggedInNotification` that causes an `AuditLog` row with `Action="UserLoggedIn", EntityType="User", EntityId=<userId>` to be persisted

#### Scenario: Password expired flag is returned when applicable
- **WHEN** `POST /api/auth/login` succeeds for a user whose password is expired (`user.IsPasswordExpired()` returns `true`)
- **THEN** the response SHALL include `PasswordExpired: true`

### Requirement: Anti-enumeration
The system SHALL return the same response shape, status code, and message for an unknown email as for an invalid password to prevent account enumeration.

#### Scenario: Unknown email and invalid password are indistinguishable
- **WHEN** `POST /api/auth/login` is called
- **THEN** both the "unknown email" and "invalid password" outcomes SHALL return `401 Unauthorized` with `Message="Invalid email or password"` AND a `LoginAttempt` row SHALL be persisted in both cases (email is recorded even when the user does not exist)

### Requirement: Rate limiting at the transport layer
The system SHALL enforce the configured rate limit on `POST /api/auth/login` (default: 10 requests per minute per IP) via the `Login` named policy registered in the ASP.NET Core rate limiter.

#### Scenario: Rate limit exceeded returns 429
- **WHEN** the rate-limit window for the source IP has been exhausted
- **THEN** the rate limiter SHALL short-circuit with `429 Too Many Requests` and the request SHALL NOT reach the controller or the handler
