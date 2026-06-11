## ADDED Requirements

### Requirement: Public page accessible to public role
The system SHALL provide a `/publico` route accessible only to authenticated users with `page-public:view` permission.

#### Scenario: User with public role can access the page
- **WHEN** an authenticated user with the `public` role navigates to `/publico`
- **THEN** the system SHALL render a page with a welcome message

#### Scenario: User without public role is redirected
- **WHEN** an authenticated user without `page-public:view` permission navigates to `/publico`
- **THEN** the system SHALL redirect to `/dashboard`

#### Scenario: Unauthenticated user is redirected to login
- **WHEN** an unauthenticated user navigates to `/publico`
- **THEN** the system SHALL redirect to `/login`

### Requirement: Public page content
The system SHALL display the following welcome message on the `/publico` page:

> Hola, gracias por registrarte en mi plataforma, no haremos nada raro con tus datos, ya que esta es solo una app de aprendizaje, tal vez en algún futuro verás algo muy interesante en este lugar, pero de momento solo tienes acceso a esta página.

#### Scenario: Welcome message is displayed
- **WHEN** a user with `page-public:view` permission visits `/publico`
- **THEN** the page SHALL display the exact welcome message text
- **AND** the page SHALL show the user's name (from JWT claims)
- **AND** the page SHALL have a clean, centered layout consistent with the platform design system

### Requirement: OAuth callback page
The system SHALL provide an `/oauth-callback` route that processes the redirect from Google OAuth and stores the JWT tokens.

#### Scenario: Successful OAuth callback
- **WHEN** Google redirects to `/oauth-callback#accessToken=xxx&refreshToken=yyy&expiresAt=zzz`
- **THEN** the frontend SHALL parse the hash fragment
- **AND** the frontend SHALL store the access token and refresh token in localStorage
- **AND** the frontend SHALL update the Zustand auth store
- **AND** the frontend SHALL redirect to `/publico`
- **AND** the frontend SHALL clean the hash from the URL (no tokens in browser history)

#### Scenario: OAuth callback with error
- **WHEN** Google redirects to `/oauth-callback?error=access_denied`
- **THEN** the frontend SHALL redirect to `/login` with an error message

### Requirement: Google Sign-In button on login page
The system SHALL display a "Sign in with Google" button on the login page, below the existing email/password form.

#### Scenario: Google button is visible on login page
- **WHEN** a user visits the login page
- **THEN** a Google-branded sign-in button SHALL be displayed below the email/password form
- **AND** clicking the button SHALL redirect to `GET /api/auth/google/login` (initiating the Authorization Code Flow)
- **AND** the button SHALL NOT disrupt the existing login form functionality
