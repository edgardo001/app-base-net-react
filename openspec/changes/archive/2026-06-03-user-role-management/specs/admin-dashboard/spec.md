## ADDED Requirements

### Requirement: Dashboard metrics
The system SHALL provide dashboard metrics accessible only to SuperAdmin role.

#### Scenario: Get dashboard stats
- **WHEN** GET /api/admin/dashboard is called with SuperAdmin role
- **THEN** totalUsers, activeUsers, inactiveUsers, newUsersLast7Days SHALL be returned

#### Scenario: Unauthorized dashboard access
- **WHEN** a non-SuperAdmin user calls GET /api/admin/dashboard
- **THEN** 403 Forbidden SHALL be returned

### Requirement: Audit log paginated view
The system SHALL provide a paginated view of all audit log entries.

#### Scenario: Get audit log
- **WHEN** GET /api/admin/audit-log is called
- **THEN** paginated audit log entries SHALL be returned with Action, EntityType, EntityId, Details, UserId, CreatedAt
- **THEN** default page SHALL be 1, default pageSize SHALL be 20

### Requirement: Global token revocation
The system SHALL allow revoking all tokens for all users.

#### Scenario: Revoke all tokens
- **WHEN** POST /api/admin/revoke-all-tokens is called
- **THEN** ALL refresh tokens across ALL users SHALL be revoked
- **THEN** an audit log entry SHALL be created
