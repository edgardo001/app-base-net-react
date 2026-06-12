## ADDED Requirements

### Requirement: GitHub OAuth login redirect
The system SHALL provide a `GET /api/auth/github/login` endpoint that redirects the user to GitHub's OAuth authorization URL.

#### Scenario: Successful redirect to GitHub
- **WHEN** a user visits `GET /api/auth/github/login`
- **THEN** the system SHALL generate a random 32-byte state string, store it in memory, and redirect (HTTP 302) to `https://github.com/login/oauth/authorize` with `client_id`, `redirect_uri`, `state`, `scope=read:user%20user:email`, and `response_type=code`

### Requirement: GitHub OAuth callback
The system SHALL provide a `GET /api/auth/github/callback?code={code}&state={state}` endpoint that exchanges the authorization code for user info and returns JWT tokens.

#### Scenario: Successful callback — new user
- **WHEN** the callback receives valid `code` and `state` parameters
- **THEN** the system SHALL:
  1. Validate the state from the in-memory store
  2. Exchange the code for an access token via `POST https://github.com/login/oauth/access_token`
  3. Fetch user info from `GET https://api.github.com/user` (and `GET https://api.github.com/user/emails` if email is not in primary response)
  4. Check for existing `ExternalLogin` with provider `"github"`
  5. If no external login exists AND no user with that email exists: create a new `User` with `RegistrationSource = "github"`, `EmailConfirmed = true`, and create `ExternalLogin` with provider `"github"`
  6. Assign the `public` role if not already assigned
  7. Generate JWT access token + refresh token
  8. Redirect to `{frontendUrl}/oauth-callback#accessToken=...&refreshToken=...&expiresAt=...`

#### Scenario: Successful callback — existing user by email
- **WHEN** the callback finds an existing user by email but no `ExternalLogin` link
- **THEN** the system SHALL create an `ExternalLogin` with provider `"github"` linked to the existing user (without assigning `public` role), generate tokens, and redirect to the frontend callback

#### Scenario: Successful callback — existing external login
- **WHEN** the callback finds an existing `ExternalLogin` with provider `"github"`
- **THEN** the system SHALL use the linked user, apply `MarkLogin()`, generate new tokens, and redirect to the frontend callback

#### Scenario: Invalid state parameter
- **WHEN** the state parameter is missing, expired, or doesn't match the stored state
- **THEN** the system SHALL redirect to `{frontendUrl}/login?error=github_auth_failed`

#### Scenario: GitHub API error
- **WHEN** the token exchange or user info fetch fails (network error, invalid code, GitHub API error)
- **THEN** the system SHALL redirect to `{frontendUrl}/login?error=github_auth_failed`

#### Scenario: Missing code or state
- **WHEN** the `code` or `state` query parameters are empty or missing
- **THEN** the system SHALL redirect to `{frontendUrl}/login?error=github_auth_failed`

### Requirement: GitHub user info mapping
The system SHALL map GitHub API user data to `GitHubUserInfo` with appropriate fallbacks.

#### Scenario: Full user info available
- **WHEN** GitHub returns `{ login: "octocat", name: "Octocat Name", email: "octocat@example.com", id: 12345 }`
- **THEN** the system SHALL create `GitHubUserInfo` with `ProviderId = "12345"`, `Email = "octocat@example.com"`, `FirstName = "Octocat"`, `LastName = "Name"`

#### Scenario: Only login available (no name, no email)
- **WHEN** GitHub returns `{ login: "octocat", name: null, email: null, id: 12345 }` and `/user/emails` also returns empty
- **THEN** the system SHALL create `GitHubUserInfo` with `ProviderId = "12345"`, `Email = "octocat@github.local"`, `FirstName = "octocat"`, `LastName = ""`

#### Scenario: Name is a single word
- **WHEN** GitHub returns `{ login: "octocat", name: "Octocat", ... }`
- **THEN** the system SHALL create `GitHubUserInfo` with `FirstName = "Octocat"`, `LastName = ""`

### Requirement: Rate limiting
The system SHALL apply rate limiting to GitHub OAuth endpoints.

#### Scenario: Rate limit enforced
- **WHEN** more than 10 requests to `/api/auth/github/login` or `/api/auth/github/callback` are made from the same IP within 1 minute
- **THEN** the system SHALL return HTTP 429 (Too Many Requests)

### Requirement: Frontend — Login button for GitHub
The login page SHALL display a "Continuar con GitHub" button that redirects to `/api/auth/github/login`.

#### Scenario: Clicking GitHub login button
- **WHEN** the user clicks "Continuar con GitHub"
- **THEN** the browser SHALL navigate to `/api/auth/github/login`

### Requirement: Configuration
The system SHALL read GitHub OAuth configuration from `Authentication:GitHub` section (ClientId, ClientSecret, RedirectUri).

#### Scenario: Missing configuration
- **WHEN** `ClientId` or `ClientSecret` are empty
- **THEN** the `GitHubAuthService` SHALL use empty strings (as configured) — the OAuth flow will fail at GitHub's side with an appropriate error

### Requirement: Registration source tracking
Users created via GitHub OAuth SHALL have `RegistrationSource = "github"`.

#### Scenario: Registration source set
- **WHEN** a new user is created via GitHub OAuth
- **THEN** the `User.RegistrationSource` SHALL be set to `"github"`

### Requirement: Public role assignment for new OAuth users
New users created via GitHub OAuth SHALL receive the `public` role, granting access to `page-public:view`.

#### Scenario: Public role assigned
- **WHEN** a new user is created via GitHub OAuth
- **THEN** the system SHALL assign the `public` role to the user (if the role exists)

#### Scenario: Public role already assigned
- **WHEN** a user created via GitHub OAuth already has the `public` role (e.g., via Google OAuth first)
- **THEN** the system SHALL NOT duplicate the role assignment

### Requirement: No password for OAuth users
Users created via GitHub OAuth SHALL NOT have a password and SHALL use OAuth for all subsequent logins.

#### Scenario: Password hash is null
- **WHEN** a user is created via GitHub OAuth
- **THEN** `User.PasswordHash` SHALL be `null`

### Requirement: GitHub OAuth setup instructions in README
The README.md SHALL include a "GitHub OAuth 2.0 — Configuración" section with step-by-step instructions.

#### Scenario: Instructions present
- **WHEN** a developer reads the README
- **THEN** there SHALL be clear instructions on how to create a GitHub OAuth App, obtain Client ID and Client Secret, configure the callback URL, and set environment variables
