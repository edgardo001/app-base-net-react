## ADDED Requirements

### Requirement: Dashboard page
The system SHALL have a dashboard page with metrics and recent activity.

#### Scenario: Dashboard metrics
- **WHEN** the dashboard loads
- **THEN** it SHALL fetch metrics from /api/admin/dashboard
- **THEN** it SHALL display 4 stat cards: Total Usuarios, Usuarios Activos, Nuevos (7 días), Usuarios Inactivos

#### Scenario: Recent activity
- **WHEN** the dashboard loads
- **THEN** it SHALL fetch recent audit log from /api/admin/audit-log
- **THEN** it SHALL display the last 10 audit entries with action badge, entity type, details, and timestamp

#### Scenario: Welcome message
- **WHEN** the dashboard loads
- **THEN** it SHALL display "Bienvenido, {firstName} {lastName}"
