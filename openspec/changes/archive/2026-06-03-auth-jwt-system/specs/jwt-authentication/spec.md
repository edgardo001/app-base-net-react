## ADDED Requirements

### Requirement: JWT access token generation
The system SHALL generate JWT access tokens using HS512 algorithm with configurable expiration.

#### Scenario: Successful token generation
- **WHEN** a user authenticates successfully
- **THEN** an access token SHALL be generated with claims: sub (userId), email, jti (unique), firstName, lastName, and permission claims
- **THEN** the token SHALL expire after the configured AccessTokenExpirationMinutes (default 15)

#### Scenario: Token validation
- **WHEN** a request includes a JWT in the Authorization header
- **THEN** the system SHALL validate: issuer, audience, lifetime, and signing key (HS512 symmetric)
- **THEN** clock skew SHALL be configurable (default 0 seconds)

#### Scenario: Invalid token rejection
- **WHEN** a request includes an expired or invalid JWT
- **THEN** the system SHALL return 401 Unauthorized

### Requirement: JWT claims mapping
The system SHALL use clean JWT claims (no Microsoft legacy mappings).

#### Scenario: Default inbound claim map cleared
- **WHEN** JWT authentication is configured
- **THEN** `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap` SHALL be cleared
- **THEN** `MapInboundClaims` SHALL be set to false
