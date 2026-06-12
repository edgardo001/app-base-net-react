## ADDED Requirements

### Requirement: Liveness endpoint
The system SHALL expose a liveness probe at `GET /health/live` that returns 200 OK as long as the process is running.

#### Scenario: Process is alive
- **WHEN** a request is made to `GET /health/live`
- **THEN** the system returns HTTP 200 with content "Healthy"

### Requirement: Readiness endpoint
The system SHALL expose a readiness probe at `GET /health/ready` that returns 200 OK only when all critical dependencies (PostgreSQL) are available.

#### Scenario: Database is connected
- **WHEN** a request is made to `GET /health/ready` and the database is responsive
- **THEN** the system returns HTTP 200 with content "Healthy"

#### Scenario: Database is disconnected
- **WHEN** a request is made to `GET /health/ready` and the database is not responsive
- **THEN** the system returns HTTP 503 with content "Unhealthy"

### Requirement: Admin health dashboard
The system SHALL expose `GET /api/admin/health` that returns detailed status of all health checks, accessible only by users with `admin:dashboard` permission.

#### Scenario: Authorized request returns detailed health
- **WHEN** an authenticated admin requests `GET /api/admin/health`
- **THEN** the system returns a JSON object with the status of each health check (database, process)
