## ADDED Requirements

### Requirement: Permissions page displays all permissions grouped by module
The system SHALL display a full permissions page at `/permissions` showing all permissions from the catalog.

#### Scenario: Permissions loaded
- **WHEN** user navigates to `/permissions`
- **THEN** the system SHALL fetch `GET /api/permissions/modules` and display permissions grouped by module in a table or card layout

#### Scenario: Permission details
- **WHEN** permissions are displayed
- **THEN** each permission SHALL show its code, name, and description

#### Scenario: Empty state
- **WHEN** no permissions exist
- **THEN** the system SHALL display "No permissions found"
