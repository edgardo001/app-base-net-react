## ADDED Requirements

### Requirement: PBKDF2 password hashing
The system SHALL hash passwords using PBKDF2 with SHA-256, 128-bit salt, 256-bit key, and 100,000 iterations.

#### Scenario: Password hashing
- **WHEN** a password is set or changed
- **THEN** PBKDF2 SHALL be used with 128-bit cryptographically random salt and 100,000 iterations
- **THEN** the stored format SHALL be "{Base64(salt)}.{Base64(hash)}"

#### Scenario: Password verification
- **WHEN** verifying a password against a stored hash
- **THEN** the salt SHALL be extracted from the stored hash
- **THEN** the key SHALL be re-derived with the same salt and iterations
- **THEN** comparison SHALL be constant-time (CryptographicOperations.FixedTimeEquals)

### Requirement: Password policy validation
The system SHALL validate passwords against configurable rules.

#### Scenario: Password length validation
- **WHEN** a password is set that is shorter than RequiredLength (default 10)
- **THEN** validation SHALL fail with "Password must be at least {length} characters"

#### Scenario: Complexity validation
- **WHEN** a password does not meet enabled complexity rules
- **THEN** validation SHALL fail with appropriate error messages for each failed rule

#### Scenario: Account lockout
- **WHEN** consecutive failed login attempts reach MaxFailedAccessAttempts (default 5)
- **THEN** the account SHALL be locked for DefaultLockoutMinutes (default 15)

#### Scenario: Password expiration
- **WHEN** a user logs in and LastPasswordChangeAt exceeds ExpirationDays (default 30)
- **THEN** the system SHALL return passwordExpired flag to force password change

### Requirement: Login with credential validation
The system SHALL validate login credentials against stored password hash and account state.

#### Scenario: Successful login
- **WHEN** valid email and password are provided
- **THEN** the system SHALL verify the password against stored hash
- **THEN** it SHALL check account is active and not locked
- **THEN** it SHALL generate access + refresh tokens
- **THEN** it SHALL record login attempt and audit log entry

#### Scenario: Failed login with invalid password
- **WHEN** an invalid password is provided
- **THEN** the system SHALL increment AccessFailedCount
- **THEN** it SHALL record a failed login attempt
- **THEN** it SHALL return 401 Unauthorized

#### Scenario: Locked account login
- **WHEN** the account is locked (LockoutEnd > UtcNow)
- **THEN** the system SHALL return 423 Locked with "Account is locked" message

#### Scenario: Inactive account login
- **WHEN** the account is not active (IsActive = false)
- **THEN** the system SHALL return 403 Forbidden with "Account is deactivated"

### Requirement: Password change
The system SHALL allow authenticated users to change their password with current password verification.

#### Scenario: Successful password change
- **WHEN** current password, new password, and confirmation match
- **THEN** the system SHALL verify current password
- **THEN** it SHALL validate new password against policy
- **THEN** it SHALL hash and store new password
- **THEN** it SHALL regenerate SecurityStamp (invalidating existing tokens)
- **THEN** it SHALL revoke all other sessions except the current one
- **THEN** it SHALL log audit entry

### Requirement: Forgot password flow
The system SHALL allow password reset via temporary password generation.

#### Scenario: Forgot password request
- **WHEN** a forgot password request is made with a valid email
- **THEN** the system SHALL generate a random 12-character temporary password
- **THEN** it SHALL hash and store the temporary password
- **THEN** it SHALL force password change on next login
- **THEN** it SHALL always return success (prevents email enumeration)

### Requirement: Logout
The system SHALL allow authenticated users to log out by revoking their current refresh token.

#### Scenario: Successful logout
- **WHEN** a logout request is made with the current refresh token
- **THEN** the refresh token SHALL be revoked
- **THEN** an audit log entry SHALL be created
