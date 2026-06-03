## ADDED Requirements

### Requirement: Roles management page
The system SHALL have a roles page with CRUD and permission assignment.

#### Scenario: Roles list as cards
- **WHEN** the roles page loads
- **THEN** roles SHALL be displayed as cards with name, description, and system badge

#### Scenario: Create role
- **WHEN** "Nuevo Rol" is clicked
- **THEN** a modal SHALL open with name and description fields

#### Scenario: Edit role with permissions
- **WHEN** edit is clicked on a role
- **THEN** a modal SHALL open with name, description, and permission toggles grouped by module
- **THEN** each permission SHALL be toggleable via clickable badges

#### Scenario: System role protection
- **WHEN** trying to delete a system role
- **THEN** an alert SHALL show "No se puede eliminar un rol del sistema"
- **THEN** the delete button for system roles SHALL be disabled
