## ADDED Requirements

### Requirement: Confirmation Link Points To Configured Frontend URL
The system SHALL compose the email confirmation link as `{EmailOptions.FrontendBaseUrl.TrimEnd('/')}/confirm-email?token={token}` so that the link lands on the frontend SPA, not on the API server.

#### Scenario: Default frontend URL is used in dev
- **WHEN** `POST /api/users` is invoked and `Email:FrontendBaseUrl` is not explicitly configured (e.g. local dev with the bundled `appsettings.json`)
- **THEN** the email body MUST contain the link `http://localhost:5173/confirm-email?token={token}` and MUST NOT contain the API host (`Request.Host`)

#### Scenario: Configured frontend URL is honoured in any environment
- **WHEN** `Email:FrontendBaseUrl` is set to any value (e.g. `https://app.example.com`)
- **THEN** the email body MUST contain the link `https://app.example.com/confirm-email?token={token}` regardless of the request's `Host` header or scheme, and MUST NOT contain `Request.Host` or `Request.Scheme`

#### Scenario: Trailing slash in the configured URL is normalised
- **WHEN** `Email:FrontendBaseUrl` is configured with a trailing `/` (e.g. `https://app.example.com/`)
- **THEN** the rendered link MUST be `https://app.example.com/confirm-email?token={token}` (no double slash)

### Requirement: Frontend Provides A Public Confirm-Email Page
The system SHALL expose a public route at `/confirm-email` in the SPA that, when opened with a `?token={token}` query string, POSTs the token to the backend's `POST /api/auth/confirm-email` endpoint and displays the outcome to the user.

#### Scenario: Valid token confirms the email
- **WHEN** the user opens `https://{frontend}/confirm-email?token={validToken}`
- **THEN** the page MUST POST `{ token }` to `/api/auth/confirm-email`, display a "Correo confirmado" success state with a link to `/login`, and the backend MUST mark the user as `EmailConfirmed`

#### Scenario: Missing token shows the error state without an API call
- **WHEN** the user opens `https://{frontend}/confirm-email` with no `?token=` query string
- **THEN** the page MUST render the error state ("Enlace inválido") and MUST NOT POST to the backend

#### Scenario: Invalid or expired token shows the error state with the backend message
- **WHEN** the user opens `https://{frontend}/confirm-email?token={invalidOrExpiredToken}`
- **THEN** the page MUST render the error state with the backend's error message, MUST NOT crash, and MUST provide a link back to `/login`

#### Scenario: Route is public
- **WHEN** the SPA route table is built
- **THEN** `/confirm-email` MUST be registered outside any authentication guard (the user is not yet logged in at this point)
