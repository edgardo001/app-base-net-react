## ADDED Requirements

### Requirement: List users by role
The system SHALL return all users assigned to a specific role via `GET /api/roles/{id}/users`.

#### Scenario: Successful listing
- **WHEN** `GET /api/roles/{id}/users` is called with a valid role ID
- **THEN** the system SHALL return `200 OK` with a list of users (id, email, firstName, lastName, avatarPath, isActive, emailConfirmed) assigned to that role

#### Scenario: Role not found
- **WHEN** `GET /api/roles/{id}/users` is called with a non-existent role ID
- **THEN** the system SHALL return `404 Not Found`

#### Scenario: Role with no users
- **WHEN** `GET /api/roles/{id}/users` is called for a role with no assigned users
- **THEN** the system SHALL return `200 OK` with an empty array

#### Scenario: Excludes soft-deleted users
- **WHEN** `GET /api/roles/{id}/users` is called
- **THEN** the system SHALL only return users where `DeletedAt IS NULL` (handled by global query filters)
