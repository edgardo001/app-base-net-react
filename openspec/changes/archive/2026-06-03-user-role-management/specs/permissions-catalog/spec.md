## ADDED Requirements

### Requirement: List all permissions
The system SHALL provide a read-only catalog of all permissions.

#### Scenario: Get all permissions
- **WHEN** GET /api/permissions is called
- **THEN** all permissions SHALL be returned with Id, Code, Name, Module, Description

### Requirement: Permissions grouped by module
The system SHALL return permissions organized by module for structured UI display.

#### Scenario: Get permissions by module
- **WHEN** GET /api/permissions/modules is called
- **THEN** permissions SHALL be grouped by Module field
- **THEN** each group SHALL contain the module name and its permissions
