## ADDED Requirements

### Requirement: Axios instance with auth interceptors
The system SHALL have a preconfigured Axios instance with request and response interceptors.

#### Scenario: Bearer token injection
- **WHEN** any request is made
- **THEN** the request interceptor SHALL read accessToken from localStorage
- **THEN** it SHALL set Authorization header to "Bearer {token}"

#### Scenario: Automatic token refresh on 401
- **WHEN** a 401 response is received
- **THEN** the response interceptor SHALL POST /api/auth/refresh with refreshToken from localStorage
- **THEN** on success, SHALL save new tokens and retry the original request

#### Scenario: Concurrent 401 queue
- **WHEN** multiple requests receive 401 simultaneously
- **THEN** only ONE refresh request SHALL be made
- **THEN** subsequent requests SHALL queue and retry with the new token

#### Scenario: Refresh failure redirect
- **WHEN** the refresh request itself fails
- **THEN** the interceptor SHALL clear localStorage
- **THEN** the interceptor SHALL redirect to /login
