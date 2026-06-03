## ADDED Requirements

### Requirement: Standardized API response wrapper
All API responses SHALL use the ApiResponse<T> wrapper for consistency.

#### Scenario: Success response
- **WHEN** an API endpoint returns successfully
- **THEN** the response SHALL be wrapped in ApiResponse with Data, Message (optional), and Success=true

#### Scenario: Error response
- **WHEN** an API endpoint returns an error
- **THEN** the response SHALL include StatusCode, Message, and Errors (optional)

### Requirement: Paginated response format
Paginated endpoints SHALL use PagedResponse<T> format.

#### Scenario: Paginated list response
- **WHEN** a paginated endpoint returns data
- **THEN** the response SHALL include Items, TotalCount, Page, PageSize, TotalPages
