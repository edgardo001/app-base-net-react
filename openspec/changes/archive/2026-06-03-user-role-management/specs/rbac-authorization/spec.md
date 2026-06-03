## ADDED Requirements

### Requirement: Role-based access control
The system SHALL restrict endpoint access based on user roles.

#### Scenario: SuperAdmin endpoint access
- **WHEN** an endpoint with [Authorize(Roles = "SuperAdmin")] is called by a SuperAdmin
- **THEN** access SHALL be granted

#### Scenario: Non-SuperAdmin blocked from admin
- **WHEN** an admin endpoint is called by a non-SuperAdmin user
- **THEN** 403 Forbidden SHALL be returned

#### Scenario: Authenticated endpoint access
- **WHEN** an endpoint with [Authorize] is called by an authenticated user
- **THEN** access SHALL be granted regardless of specific roles

#### Scenario: Unauthenticated access blocked
- **WHEN** an endpoint with [Authorize] is called without a valid JWT
- **THEN** 401 Unauthorized SHALL be returned

### Requirement: Permission-based authorization
The system SHALL include user permissions in JWT claims for frontend authorization.

#### Scenario: Permissions in login response
- **WHEN** a user logs in
- **THEN** the response SHALL include the user's permission codes aggregated from all assigned roles

#### Scenario: Permission claim in JWT
- **WHEN** a JWT is generated
- **THEN** the token SHALL include permission claims for all permissions the user has via roles
