## ADDED Requirements

### Requirement: Admin audit page
The system SHALL have an admin page with audit log and session management.

#### Scenario: Audit log table
- **WHEN** the admin page loads
- **THEN** it SHALL fetch paginated audit log from /api/admin/audit-log
- **THEN** the table SHALL show columns: Acción, Tipo, Detalle, Fecha
- **THEN** pagination controls SHALL be available

#### Scenario: Revoke all sessions
- **WHEN** "Revocar Todas las Sesiones" is clicked
- **THEN** a confirm dialog SHALL appear with warning message
- **THEN** on confirm, SHALL POST /api/admin/revoke-all-tokens
- **THEN** on success, SHALL show alert and refresh audit log
