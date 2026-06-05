# auth-logout Specification

## Purpose
TBD - created by archiving change cqrs-auth-refresh. Update Purpose after archive.
## Requirements
### Requirement: Logout endpoint revokes the current session's refresh token
The system SHALL revoke the single refresh token presented in the request body and write a `UserLoggedOut` audit entry. The endpoint is idempotent: an unknown token still returns 200.

#### Scenario: Logout with valid token revokes and audits
- **WHEN** `POST /api/auth/logout` is called with a refresh token that exists in the store
- **THEN** the system SHALL mark the token revoked, persist the change, publish a `UserLoggedOutNotification` (which writes a `UserLoggedOut` audit row), and return `200 OK` with `Message="Logged out successfully"`

#### Scenario: Logout with unknown token is a no-op success
- **WHEN** `POST /api/auth/logout` is called with a refresh token that does not exist
- **THEN** the system SHALL return `200 OK` with `Message="Logged out successfully"` and SHALL NOT throw

