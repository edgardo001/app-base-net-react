## ADDED Requirements

### Requirement: Google OAuth Authorization Code Flow
The system SHALL implement the Authorization Code Flow for Google OAuth2 authentication. The backend SHALL initiate the flow, receive the callback, exchange the authorization code for tokens, and verify the ID token before creating or authenticating a user.

#### Scenario: Backend generates Google OAuth URL
- **WHEN** an unauthenticated user requests `GET /api/auth/google/login`
- **THEN** the backend SHALL generate a Google OAuth authorization URL with `response_type=code`, `client_id`, `redirect_uri`, `scope=openid email profile`, `access_type=offline`, and a `state` parameter containing a cryptographically random nonce
- **AND** the backend SHALL store the `state` nonce temporarily for CSRF validation
- **AND** the backend SHALL return the authorization URL to the client (302 redirect)

#### Scenario: Backend validates state parameter
- **WHEN** Google redirects to the backend callback `GET /api/auth/google/callback` with a `code` and `state` parameter
- **THEN** the backend SHALL verify the `state` parameter matches the stored nonce
- **AND** if the `state` is invalid or missing, the backend SHALL reject the request with 400 Bad Request

#### Scenario: Backend exchanges authorization code
- **WHEN** the `state` is valid
- **THEN** the backend SHALL exchange the authorization code for tokens by calling `POST https://oauth2.googleapis.com/token` with `grant_type=authorization_code`, `client_id`, `client_secret`, `code`, and `redirect_uri`
- **AND** the backend SHALL receive an `id_token`, `access_token`, and `refresh_token` from Google

#### Scenario: Backend verifies ID token
- **WHEN** the backend receives the `id_token` from Google
- **THEN** the backend SHALL verify the ID token using Google's public JWKS keys
- **AND** the backend SHALL validate the `aud` claim matches the configured `ClientId`
- **AND** the backend SHALL validate the `iss` claim equals `https://accounts.google.com` or `accounts.google.com`
- **AND** the backend SHALL validate the token has not expired (`exp` claim)

#### Scenario: Backend finds or creates user from ID token
- **WHEN** the ID token is verified successfully
- **THEN** the backend SHALL extract `sub` (Google User ID), `email`, `given_name`, and `family_name` from the ID token claims
- **AND** the backend SHALL look for an existing `ExternalLogin` with `Provider="google"` and `ProviderId=<sub>`
- **OR** the backend SHALL look for an existing `User` with matching email and link the account (create `ExternalLogin`)
- **AND** if no matching user is found, the backend SHALL create a new `User` with `EmailConfirmed=true`, `IsActive=true`, `PasswordHash=null`, and `ExternalLogins` containing the Google identity
- **AND** if the user already has a password (`PasswordHash != null`), the existing password login SHALL remain functional alongside Google login

#### Scenario: Backend assigns public role to new Google users
- **WHEN** a new user is created via Google OAuth
- **THEN** the backend SHALL assign the `public` role to the user
- **AND** if the user already existed (email match), the `public` role SHALL NOT be auto-assigned (preserve existing roles)

#### Scenario: Backend returns JWT and redirects to frontend
- **WHEN** the user is found or created
- **THEN** the backend SHALL generate an access token and refresh token (same as existing login flow)
- **AND** the backend SHALL redirect the user's browser to `<FRONTEND_URL>/oauth-callback#accessToken=<jwt>&refreshToken=<rt>&expiresAt=<epoch>`
- **AND** the backend SHALL NOT return the JWT in the URL query string (use hash fragment)

#### Scenario: Rate limiting on callback
- **WHEN** a client hits `GET /api/auth/google/callback` more than 10 times per minute
- **THEN** the backend SHALL return 429 Too Many Requests

#### Scenario: Rate limiting on login initiation
- **WHEN** a client hits `GET /api/auth/google/login` more than 10 times per minute
- **THEN** the backend SHALL return 429 Too Many Requests

#### Scenario: Google token verification fails
- **WHEN** the ID token verification fails (invalid signature, wrong audience, expired)
- **THEN** the backend SHALL log the failure and redirect to `<FRONTEND_URL>/login?error=google_auth_failed`

### Requirement: ExternalLogin Entity
The system SHALL store external identity provider information in a new `ExternalLogin` entity with a many-to-one relationship to `User`.

#### Scenario: ExternalLogin stores provider details
- **WHEN** a user authenticates via Google OAuth
- **THEN** an `ExternalLogin` record SHALL be created with `UserId`, `Provider="google"`, `ProviderId=<Google sub claim>`, `ProviderEmail=<email from ID token>`, and `CreatedAt`
- **AND** the combination of `Provider` + `ProviderId` SHALL be unique

#### Scenario: User with multiple providers
- **WHEN** a user links a second external provider (future)
- **THEN** the system SHALL support multiple `ExternalLogin` records per user

### Requirement: User entity supports passwordless accounts
The system SHALL allow `User.PasswordHash` to be `null` for users created via OAuth.

#### Scenario: Passwordless user cannot login via password
- **WHEN** a passwordless user attempts `POST /api/auth/login` with email and password
- **THEN** the system SHALL return 401 Unauthorized with "Invalid email or password" (same anti-enumeration message)
