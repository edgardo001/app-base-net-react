## ADDED Requirements

### Requirement: Login rejects unconfirmed emails
The system SHALL require email confirmation before allowing login.

#### Scenario: Login with confirmed email succeeds
- **WHEN** a user with `EmailConfirmed = true` logs in with valid credentials
- **THEN** the login SHALL proceed normally and return access + refresh tokens

#### Scenario: Login with unconfirmed email returns 403
- **WHEN** a user with `EmailConfirmed = false` attempts to log in
- **THEN** the system SHALL return `403 Forbidden` with "Email not confirmed. Check your inbox."

### Requirement: Confirm email validates token
The system SHALL validate the confirmation token and mark the email as confirmed.

#### Scenario: Valid confirmation token
- **WHEN** `POST /api/auth/confirm-email` is called with a valid token that matches a user and has not expired
- **THEN** the system SHALL set `EmailConfirmed = true`, clear the token and expiry, send a welcome email, and return `200 OK`

#### Scenario: Expired confirmation token
- **WHEN** `POST /api/auth/confirm-email` is called with an expired token
- **THEN** the system SHALL return `400 Bad Request` with "Confirmation token has expired"

#### Scenario: Invalid confirmation token
- **WHEN** `POST /api/auth/confirm-email` is called with a token that does not match any user
- **THEN** the system SHALL return `400 Bad Request` with "Invalid confirmation token"

### Requirement: Admin can create users with confirmed email
The system SHALL auto-confirm the email when an admin creates a user via the admin panel.

#### Scenario: Admin creates user
- **WHEN** `POST /api/users` is called by an authorized admin
- **THEN** the created user SHALL have `EmailConfirmed = true` and a welcome email SHALL be sent
