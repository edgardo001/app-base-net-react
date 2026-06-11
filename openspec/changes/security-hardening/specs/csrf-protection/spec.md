## ADDED Requirements

### Requirement: CSRF header validation on state-changing requests
The system SHALL require a valid `X-CSRF-TOKEN` header on all POST, PUT, PATCH, and DELETE requests, except for explicitly excluded routes.

#### Scenario: Missing header returns 403
- **WHEN** a POST/PUT/PATCH/DELETE request is received without an `X-CSRF-TOKEN` header
- **THEN** the system returns HTTP 403 Forbidden

#### Scenario: Valid header passes through
- **WHEN** a POST/PUT/PATCH/DELETE request includes a valid `X-CSRF-TOKEN` header
- **THEN** the request proceeds to the controller

#### Scenario: Login and refresh endpoints are excluded
- **WHEN** a request is made to `POST /api/auth/login` or `POST /api/auth/refresh`
- **THEN** the CSRF middleware does NOT validate the header

### Requirement: Frontend sends CSRF token
The frontend SHALL include the `X-CSRF-TOKEN` header on all state-changing requests via the axios interceptor.

#### Scenario: Axios interceptor adds header
- **WHEN** the frontend makes a POST/PUT/PATCH/DELETE request via axios
- **THEN** the interceptor adds `X-CSRF-TOKEN: <token-value>` header
- **AND** the token value is a random UUID generated at app startup and stored in memory

#### Scenario: GET and OPTIONS requests are not modified
- **WHEN** the frontend makes a GET or OPTIONS request via axios
- **THEN** the interceptor does NOT add the `X-CSRF-TOKEN` header
