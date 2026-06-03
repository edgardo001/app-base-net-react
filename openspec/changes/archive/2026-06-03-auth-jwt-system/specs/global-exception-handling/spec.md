## ADDED Requirements

### Requirement: Global exception handling
The system SHALL catch all unhandled exceptions and return appropriate HTTP responses with sanitized messages.

#### Scenario: Validation exception
- **WHEN** a FluentValidation.ValidationException is thrown
- **THEN** the system SHALL return 400 BadRequest with validation errors
- **THEN** the exception SHALL be logged at Warning level

#### Scenario: Unauthorized access
- **WHEN** an UnauthorizedAccessException is thrown
- **THEN** the system SHALL return 403 Forbidden

#### Scenario: Resource not found
- **WHEN** a KeyNotFoundException is thrown
- **THEN** the system SHALL return 404 NotFound with the exception message

#### Scenario: Internal server error
- **WHEN** any other unhandled exception occurs
- **THEN** the system SHALL return 500 InternalServerError with a generic message (no stack trace exposed)
- **THEN** the exception SHALL be logged at Error level with full details
