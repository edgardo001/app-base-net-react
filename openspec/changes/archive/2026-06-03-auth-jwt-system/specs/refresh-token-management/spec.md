## ADDED Requirements

### Requirement: Refresh token generation and storage
The system SHALL generate cryptographically secure refresh tokens and store only the SHA-256 hash in the database.

#### Scenario: Refresh token creation
- **WHEN** a user logs in
- **THEN** a refresh token SHALL be generated using 64 cryptographically random bytes (Base64 encoded)
- **THEN** the SHA-256 hash of the token SHALL be stored in the database, not the plain token
- **THEN** the token SHALL be associated with the user's JwtId, device info, and IP address
- **THEN** the token SHALL expire after RefreshTokenExpirationDays (default 7)

#### Scenario: Refresh token validation
- **WHEN** a refresh token is presented
- **THEN** the system SHALL hash the presented token and compare with stored hash using constant-time comparison (CryptographicOperations.FixedTimeEquals)
- **THEN** the system SHALL verify the token is not expired and not revoked

### Requirement: Token rotation
The system SHALL rotate refresh tokens on each use (issue new, revoke old).

#### Scenario: Successful rotation
- **WHEN** a valid, non-revoked refresh token is used to refresh
- **THEN** the old token SHALL be revoked with RevokedAt and ReplacedByTokenHash set
- **THEN** a new access token SHALL be issued
- **THEN** a new refresh token SHALL be issued with a new hash stored

### Requirement: Token reuse detection
The system SHALL detect if a revoked refresh token is re-presented and revoke all user sessions.

#### Scenario: Reuse detection trigger
- **WHEN** a revoked refresh token is presented (its hash matches ReplacedByTokenHash)
- **THEN** ALL refresh tokens for the user SHALL be revoked globally
- **THEN** the event SHALL be logged in AuditLog

### Requirement: Token revocation
The system SHALL support token revocation at individual and global levels.

#### Scenario: Individual user revocation
- **WHEN** an admin calls revoke-tokens for a specific user
- **THEN** ALL refresh tokens for that user SHALL be revoked

#### Scenario: Global revocation
- **WHEN** an admin calls revoke-all-tokens
- **THEN** ALL refresh tokens for ALL users SHALL be revoked
