## ADDED Requirements

### Requirement: Admin can send a test email
The system SHALL allow SuperAdmin users to send a test email to verify SMTP configuration.

#### Scenario: Successful test email
- **WHEN** a SuperAdmin enters a valid email address and clicks "Enviar Correo de Prueba"
- **THEN** the system sends an email using the configured SMTP provider
- **AND** the system returns a success response
- **AND** the frontend shows a success toast

#### Scenario: Invalid email address
- **WHEN** a SuperAdmin enters an invalid email address
- **THEN** the system returns a validation error
- **AND** the frontend shows an error toast with the validation message

#### Scenario: SMTP not configured
- **WHEN** SMTP host is not configured
- **THEN** the system returns an error response
- **AND** the frontend shows an error toast

#### Scenario: Audit logging
- **WHEN** a test email is sent
- **THEN** the system SHALL log the action "TestEmailSent" in the audit log with the recipient email
