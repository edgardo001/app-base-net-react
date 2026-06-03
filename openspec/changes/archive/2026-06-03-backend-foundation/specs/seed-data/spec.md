## ADDED Requirements

### Requirement: Database seeding on startup
The system SHALL automatically apply pending migrations and seed initial data on application startup.

#### Scenario: Auto-migration on startup
- **WHEN** the application starts
- **THEN** pending EF Core migrations SHALL be applied automatically before seed data

### Requirement: System roles seeding
The system SHALL seed 5 roles with appropriate permissions on first run.

#### Scenario: SuperAdmin role seed
- **WHEN** seed runs
- **THEN** a role named "SuperAdmin" SHALL be created with IsSystem = true
- **THEN** SuperAdmin SHALL be granted all 18 permissions

#### Scenario: Admin role seed
- **WHEN** seed runs
- **THEN** a role named "Admin" SHALL be created with IsSystem = true
- **THEN** Admin SHALL be granted users:*, roles:*, permissions:*, audit:view, admin:dashboard permissions

#### Scenario: Tipo A/B/C role seeds
- **WHEN** seed runs
- **THEN** roles "user-tipo-a", "user-tipo-b", "user-tipo-c" SHALL be created with page-specific permissions

### Requirement: Permission catalog seeding
The system SHALL seed a catalog of 18 granular permissions organized by module.

#### Scenario: Permission module structure
- **WHEN** seed runs
- **THEN** permissions SHALL be created for modules: Users (6), Roles (4), Permissions (2), Audit (1), Admin (2), Profile (3)

### Requirement: Default admin user seeding
The system SHALL seed a default SuperAdmin user with temporary password.

#### Scenario: Default admin login
- **WHEN** seed runs
- **THEN** a user with email "admin" SHALL be created
- **THEN** the user SHALL have the SuperAdmin role assigned
- **THEN** the password SHALL be set to "admin" (temporary, force change on first login)
- **THEN** `LastPasswordChangeAt` SHALL be set to null to trigger forced password change
