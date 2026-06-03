## ADDED Requirements

### Requirement: Structured logging with Serilog
The system SHALL use Serilog for structured logging with Console and File sinks.

#### Scenario: Application startup logging
- **WHEN** the application starts
- **THEN** Serilog SHALL be configured reading from appsettings.json
- **THEN** bootstrap logger SHALL be created before host builder

#### Scenario: File log rotation
- **WHEN** logs are written to file
- **THEN** they SHALL be written to `logs/log-.txt` with daily rolling interval

#### Scenario: Request logging middleware
- **WHEN** HTTP requests are processed
- **THEN** Serilog request logging middleware SHALL log request method, path, status code, and duration

### Requirement: Middleware pipeline for error handling and security
The system SHALL implement a configured middleware pipeline with global exception handling and security headers.

#### Scenario: Global exception handling
- **WHEN** an unhandled exception occurs in downstream middleware
- **THEN** ExceptionHandlingMiddleware SHALL catch it and return appropriate HTTP error response
- **THEN** the exception SHALL be logged via Serilog

#### Scenario: Security headers on all responses
- **WHEN** any HTTP response is sent
- **THEN** SecurityHeadersMiddleware SHALL add X-Frame-Options: DENY, X-Content-Type-Options: nosniff, X-XSS-Protection, Referrer-Policy, Permissions-Policy, and Content-Security-Policy headers

### Requirement: Health checks
The system SHALL implement health check endpoints for monitoring.

#### Scenario: Liveness check
- **WHEN** GET /health is called
- **THEN** the system SHALL respond with 200 OK if the process is running
