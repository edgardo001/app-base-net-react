## ADDED Requirements

### Requirement: Public role with page-public:view permission
The system SHALL define a new `public` role with `IsSystem=true` and a single permission `page-public:view`.

#### Scenario: Public role is seeded on startup
- **WHEN** the database is seeded on first run
- **THEN** the `public` role SHALL be created with `Name="public"`, `NormalizedName="PUBLIC"`, `Description="Rol público para usuarios que ingresan vía OAuth"`, `IsSystem=true`
- **AND** the `page-public:view` permission SHALL exist with `Code="page-public:view"`, `Name="Ver página pública"`, `Module="Público"`, `Description="Permite acceder a la página pública de bienvenida post-registro"`
- **AND** the `public` role SHALL have the `page-public:view` permission granted

#### Scenario: Public role cannot be deleted
- **WHEN** an admin attempts to delete the `public` role via `DELETE /api/roles/{id}`
- **THEN** the system SHALL reject the deletion with 400 Bad Request because `IsSystem=true`

#### Scenario: Public role permissions cannot be modified
- **WHEN** an admin attempts to update permissions of the `public` role via `PUT /api/roles/{id}/permissions`
- **THEN** the system SHALL return 400 Bad Request indicating system roles cannot be modified

### Requirement: New Google users auto-assigned public role
The system SHALL automatically assign the `public` role to newly created users via Google OAuth.

#### Scenario: Auto-assignment on Google registration
- **WHEN** a user registers via Google OAuth for the first time (no existing account)
- **THEN** the system SHALL assign the `public` role to the user

#### Scenario: Existing user linking Google account
- **WHEN** a user with existing roles links their Google account (email match)
- **THEN** the system SHALL NOT auto-assign the `public` role (preserve existing role assignments)
