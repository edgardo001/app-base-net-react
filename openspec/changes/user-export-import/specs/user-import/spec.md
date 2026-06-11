## ADDED Requirements

### Requirement: User import from CSV
The system SHALL provide a `POST /api/users/import` endpoint that accepts a CSV file, validates each row, and creates users in batch.

#### Scenario: Import valid CSV creates users
- **WHEN** an authorized admin uploads a CSV file with valid user data via `POST /api/users/import`
- **THEN** the system creates users for each valid row
- **AND** returns a report with `created: N`, `errors: 0`

#### Scenario: Import with invalid rows reports errors
- **WHEN** an authorized admin uploads a CSV file where some rows have invalid data (missing email, weak password, duplicate email)
- **THEN** the system creates users only for valid rows
- **AND** returns a report listing each error with row number and description

#### Scenario: File size exceeds limit
- **WHEN** an authorized admin uploads a CSV file larger than 10MB
- **THEN** the system returns HTTP 413 Payload Too Large

#### Scenario: CSV with invalid format
- **WHEN** an authorized admin uploads a file that is not a valid CSV (wrong columns, unparseable)
- **THEN** the system returns HTTP 400 with an error message describing the format issue

#### Scenario: Unauthorized request returns 403
- **WHEN** a non-admin user requests `POST /api/users/import`
- **THEN** the system returns HTTP 403 Forbidden
