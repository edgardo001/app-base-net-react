## ADDED Requirements

### Requirement: Rate limiting policies
The system SHALL enforce rate limiting per IP address using fixed window algorithm.

#### Scenario: Login rate limiting
- **WHEN** more than 10 login requests per minute come from the same IP
- **THEN** the system SHALL return 429 Too Many Requests

#### Scenario: Forgot password rate limiting
- **WHEN** more than 3 forgot password requests per hour come from the same IP
- **THEN** the system SHALL return 429 Too Many Requests

#### Scenario: Global API rate limiting
- **WHEN** more than 100 requests per minute come from the same IP
- **THEN** the system SHALL return 429 Too Many Requests

### Requirement: Rate limiter pipeline placement
The rate limiter middleware SHALL be placed before authentication to reject abusive traffic early.

#### Scenario: Pipeline ordering
- **WHEN** the middleware pipeline is configured
- **THEN** UseRateLimiter SHALL be registered after CORS and before Authentication
