## ADDED Requirements

### Requirement: Email templates use string interpolation
The system SHALL support `{{VariableName}}` syntax in HTML template files for variable substitution.

#### Scenario: Template renders with all variables
- **WHEN** a template contains `{{UserName}}`, `{{ConfirmationLink}}`, `{{TempPassword}}`, `{{ResetLink}}`, or `{{Year}}`
- **THEN** all variables SHALL be replaced with their values

#### Scenario: Missing variable throws
- **WHEN** a template references a variable that is not provided in the data dictionary
- **THEN** the renderer SHALL throw `InvalidOperationException` with the missing variable name

### Requirement: Welcome email template
The system SHALL have a `welcome.html` template sent after email confirmation.

#### Scenario: Welcome email sent after confirmation
- **WHEN** a user confirms their email
- **THEN** the welcome email SHALL include the user's name, a login link, and branding

### Requirement: Email confirmation template
The system SHALL have an `email-confirmation.html` template for email verification.

#### Scenario: Confirmation email sent at registration
- **WHEN** a user is created (self-registration)
- **THEN** the confirmation email SHALL include a confirmation link with the token

### Requirement: Password reset template
The system SHALL have a `password-reset.html` template for reset password flow.

#### Scenario: Reset email sent on forgot-password
- **WHEN** a user requests a password reset
- **THEN** the reset email SHALL include a reset link with the token

### Requirement: Password changed notification template
The system SHALL have a `password-changed.html` template.

#### Scenario: Change notification sent
- **WHEN** a user changes their password
- **THEN** the notification email SHALL inform the user of the change and provide a contact link if they did not request it

### Requirement: Temporary password template
The system SHALL have a `temporary-password.html` template for admin-generated temp passwords.

#### Scenario: Temp password sent by admin
- **WHEN** an admin resets a user's password
- **THEN** the email SHALL include the temporary password and instructions to change it on first login

### Requirement: Account locked notification template
The system SHALL have an `account-locked.html` template.

#### Scenario: Lock notification sent
- **WHEN** a user's account is locked due to failed login attempts
- **THEN** the notification email SHALL inform the user of the lockout and provide instructions to reset their password

### Requirement: Templates are responsive HTML
All email templates SHALL be responsive HTML with inline CSS, compatible with major email clients (Gmail, Outlook, Apple Mail).

#### Scenario: Templates render across email clients
- **WHEN** any template is rendered
- **THEN** it SHALL use inline CSS, table-based layout, and responsive design for mobile viewports
