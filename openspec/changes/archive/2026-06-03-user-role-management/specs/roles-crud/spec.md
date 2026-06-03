## ADDED Requirements

### Requirement: List roles
The system SHALL list all roles with permissions.

#### Scenario: Get all roles
- **WHEN** GET /api/roles is called
- **THEN** all roles SHALL be returned with Id, Name, Description, IsSystem, CreatedAt

### Requirement: Get role with permissions
The system SHALL return a role with its permission assignments.

#### Scenario: Get role detail
- **WHEN** GET /api/roles/{id} is called
- **THEN** the role SHALL be returned with its permission assignments (PermissionId, Code, Granted)

### Requirement: Create role
The system SHALL allow creating new roles.

#### Scenario: Successful role creation
- **WHEN** POST /api/roles is called with name and description
- **THEN** the role SHALL be created
- **THEN** IsSystem SHALL be false by default
- **THEN** the creation SHALL be logged in AuditLog

### Requirement: Update role
The system SHALL allow updating role name and description, but NOT for system roles.

#### Scenario: Update non-system role
- **WHEN** PUT /api/roles/{id} is called for a non-system role
- **THEN** name and description SHALL be updated

#### Scenario: Update system role blocked
- **WHEN** PUT /api/roles/{id} is called for a system role (IsSystem = true)
- **THEN** 422 Unprocessable Entity SHALL be returned

### Requirement: Delete role
The system SHALL allow deleting roles, but NOT system roles.

#### Scenario: Delete non-system role
- **WHEN** DELETE /api/roles/{id} is called for a non-system role
- **THEN** the role SHALL be deleted

#### Scenario: Delete system role blocked
- **WHEN** DELETE /api/roles/{id} is called for a system role
- **THEN** 422 Unprocessable Entity SHALL be returned

### Requirement: Update role permissions
The system SHALL allow replacing all permission assignments for a role.

#### Scenario: Permission update
- **WHEN** PATCH /api/roles/{id}/permissions is called with array of {permissionId, granted}
- **THEN** all existing role permissions SHALL be cleared
- **THEN** new role permissions SHALL be inserted
- **THEN** the change SHALL be logged in AuditLog
