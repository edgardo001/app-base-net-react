## ADDED Requirements

### Requirement: Soft-delete a user
The system SHALL soft-delete a user by setting `DeletedAt` to the current UTC timestamp. The user SHALL be excluded from all queries via global query filters.

#### Scenario: Successful soft-delete
- **WHEN** `DELETE /api/users/{id}` is called with a valid user ID and the user is not a system user
- **THEN** the system SHALL set `DeletedAt` to the current UTC timestamp, save the audit log entry, and return `200 OK`

#### Scenario: User not found
- **WHEN** `DELETE /api/users/{id}` is called with a non-existent user ID
- **THEN** the system SHALL return `404 Not Found`

#### Scenario: Cannot soft-delete system user
- **WHEN** `DELETE /api/users/{id}` is called for a user with `IsSystem = true` on any of their roles
- **THEN** the system SHALL return `403 Forbidden` with message "Cannot delete system users"

#### Scenario: Cannot soft-delete self
- **WHEN** `DELETE /api/users/{id}` is called where `id` matches the authenticated user's ID
- **THEN** the system SHALL return `400 Bad Request` with message "Cannot delete your own account"
