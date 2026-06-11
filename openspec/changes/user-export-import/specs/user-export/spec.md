## ADDED Requirements

### Requirement: User export to CSV
The system SHALL provide a `GET /api/users/export` endpoint that returns a CSV file with the same filters available in the user list endpoint (`GET /api/users`).

#### Scenario: Export all filtered users
- **WHEN** an authorized admin requests `GET /api/users/export?search=john&isActive=true`
- **THEN** the system returns a CSV file with Content-Type `text/csv`
- **AND** the CSV includes columns: Email, FirstName, LastName, IsActive, EmailConfirmed, Roles, CreatedAt

#### Scenario: Export without filters exports all
- **WHEN** an authorized admin requests `GET /api/users/export` without query parameters
- **THEN** the system returns a CSV file with ALL active users (soft-deleted excluded)

#### Scenario: Unauthorized request returns 403
- **WHEN** a non-admin user requests `GET /api/users/export`
- **THEN** the system returns HTTP 403 Forbidden
