# auth-refresh Specification

## Purpose
TBD - created by archiving change cqrs-auth-refresh. Update Purpose after archive.
## Requirements
### Requirement: Refresh endpoint rotates tokens and reuses the refresh token at most once
The system SHALL rotate the refresh token on every successful call to `POST /api/auth/refresh`. The old refresh token SHALL be revoked; a new refresh token (with a fresh 7-day expiry) and a new access token SHALL be issued. The new refresh token SHALL be the only valid one for the user's next refresh.

#### Scenario: Valid refresh returns a new token pair
- **WHEN** `POST /api/auth/refresh` is called with a refresh token that exists, is not revoked, has not expired, and belongs to an active user
- **THEN** the system SHALL revoke the old refresh token, persist a new `RefreshToken` row with `ExpiresAt = UtcNow + 7 days`, return `200 OK` with `{ AccessToken, RefreshToken, ExpiresAt }`, and publish a `TokenRefreshedNotification`

### Requirement: Refresh rejects invalid, revoked, and expired tokens
The system SHALL reject any refresh-token reuse, token-revocation, or token-expiry case with `401 Unauthorized` and a stable error message; reuse detection SHALL additionally revoke every refresh token belonging to the affected user.

#### Scenario: Unknown token returns 401
- **WHEN** `POST /api/auth/refresh` is called with a refresh token whose hash does not match any stored token
- **THEN** the system SHALL return `401 Unauthorized` with `Message="Invalid refresh token"`

#### Scenario: Reused (revoked) token triggers full-session revocation
- **WHEN** `POST /api/auth/refresh` is called with a refresh token whose stored row is already revoked
- **THEN** the system SHALL call `IRefreshTokenRepository.RevokeAllForUserAsync(userId, null)`, persist with `SaveChangesAsync`, publish a `TokenReuseDetectedNotification`, and return `401 Unauthorized` with `Message="Token compromised. All sessions revoked."`

#### Scenario: Expired token returns 401
- **WHEN** `POST /api/auth/refresh` is called with a refresh token whose `ExpiresAt` is in the past
- **THEN** the system SHALL return `401 Unauthorized` with `Message="Refresh token expired"`

#### Scenario: User inactive or missing returns 401
- **WHEN** `POST /api/auth/refresh` is called with a valid, non-revoked, non-expired refresh token belonging to a user that is `null`, soft-deleted, or `IsActive=false`
- **THEN** the system SHALL return `401 Unauthorized` with `Message="User not found or inactive"`

### Requirement: Refresh writes a TokenRefreshed audit entry on success
The system SHALL persist an `AuditLog` row with `Action="TokenRefreshed"` and `EntityType="RefreshToken"` whenever a refresh-token rotation completes successfully.

#### Scenario: Successful refresh records audit
- **WHEN** a refresh-token rotation completes successfully
- **THEN** the system SHALL publish a `TokenRefreshedNotification` that causes an `AuditLog` row to be persisted with the rotating token's id, the user's id, the source IP, and the user agent

### Requirement: Reuse-detection writes a TokenReuseDetected audit entry
The system SHALL persist an `AuditLog` row with `Action="TokenReuseDetected"` and `EntityType="RefreshToken"` whenever token-reuse detection fires.

#### Scenario: Reuse detection records audit and all-sessions revocation
- **WHEN** the reuse-detection path fires
- **THEN** the system SHALL publish a `TokenReuseDetectedNotification` that causes an `AuditLog` row to be persisted with the offending token's id, the affected user id, the source IP, and `Details="Compromised refresh token detected — all sessions revoked"`

### Requirement: Logout revokes a single refresh token and writes an audit entry
The system SHALL revoke the refresh token whose hash matches the request, write a `UserLoggedOut` audit entry, and return `200 OK` — even when the token does not exist (idempotent).

#### Scenario: Known token is revoked and audited
- **WHEN** `POST /api/auth/logout` is called with a refresh token that matches a stored row
- **THEN** the system SHALL mark the stored row revoked, persist via `SaveChangesAsync`, publish a `UserLoggedOutNotification`, and return `200 OK` with `Message="Logged out successfully"`

#### Scenario: Unknown token is a no-op success
- **WHEN** `POST /api/auth/logout` is called with a refresh token that does not match any stored row
- **THEN** the system SHALL return `200 OK` with `Message="Logged out successfully"` and SHALL NOT throw, audit, or modify state

