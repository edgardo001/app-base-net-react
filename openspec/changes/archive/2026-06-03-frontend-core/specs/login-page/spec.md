## ADDED Requirements

### Requirement: Login page
The system SHALL have a login page with email/password form.

#### Scenario: Successful login
- **WHEN** valid credentials are submitted
- **THEN** the form SHALL call login action from auth store
- **THEN** on success with password not expired, SHALL redirect to /dashboard
- **THEN** on success with password expired, SHALL redirect to /change-password

#### Scenario: Login error display
- **WHEN** login fails
- **THEN** the error message from the API SHALL be displayed in a styled error div
- **THEN** if no API error message, SHALL show generic "Error al iniciar sesión"

#### Scenario: Loading state
- **WHEN** login is in progress
- **THEN** the submit button SHALL be disabled and show "Ingresando..."
