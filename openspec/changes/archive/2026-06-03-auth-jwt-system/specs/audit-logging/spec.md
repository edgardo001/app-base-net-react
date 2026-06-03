## ADDED Requirements

### Requirement: Audit logging for security events
The system SHALL log all security-critical operations to the AuditLog table.

#### Scenario: Login audit
- **WHEN** a login attempt occurs (successful or failed)
- **THEN** an AuditLog entry SHALL be created with Action, UserId, IpAddress, UserAgent, and Details

#### Scenario: Password change audit
- **WHEN** a user changes their password
- **THEN** an AuditLog entry SHALL be created with Action = "PasswordChanged"

#### Scenario: Token revocation audit
- **WHEN** tokens are revoked (individual, global, or reuse detection)
- **THEN** an AuditLog entry SHALL be created with Action describing the revocation scope

### Requirement: Login attempt tracking
The system SHALL record all login attempts (successful and failed) in the LoginAttempt table.

#### Scenario: Failed login tracking
- **WHEN** a login attempt fails
- **THEN** a LoginAttempt SHALL be recorded with Email, IpAddress, Success=false, and FailureReason
