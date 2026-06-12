## ADDED Requirements

### Requirement: Admin metrics endpoint
The system SHALL expose `GET /api/admin/metrics` that returns runtime metrics of the application process.

#### Scenario: Authorized request returns metrics
- **WHEN** an authenticated admin requests `GET /api/admin/metrics`
- **THEN** the system returns a JSON object with:
  - `uptime`: process uptime in seconds
  - `memoryBytes`: current memory usage in bytes
  - `totalRequests`: total HTTP requests handled since process start
  - `gcCollections`: number of garbage collections per generation
  - `threadPoolThreads`: active thread pool threads

#### Scenario: Unauthorized request returns 403
- **WHEN** a non-admin user requests `GET /api/admin/metrics`
- **THEN** the system returns HTTP 403 Forbidden
