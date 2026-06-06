# user-creation Specification

## Purpose
TBD - created by archiving change user-creation-email-confirmation. Update Purpose after archive.
## Requirements
### Requirement: Create User Persists With Unconfirmed Email
The system SHALL create a new user when an admin posts to `POST /api/users` with a valid `CreateUserRequest`, persist the user with `EmailConfirmed = false`, assign any provided `RoleIds` to the new user, and return HTTP 201 with an `ApiResponse<object>` containing the new user's `id` and `email`.

#### Scenario: Valid request creates the user
- **WHEN** an admin posts a `CreateUserRequest` with a unique email, first name, last name, password, and optional `RoleIds`
- **THEN** the system MUST hash the password, create the `User` via `User.Create(...)`, add any requested `UserRole` associations, persist via `IUnitOfWork.Users.AddAsync` + `IUnitOfWork.SaveChangesAsync`, and return HTTP 201 with `ApiResponse<object>.Ok(new { id, email })`

#### Scenario: Created user is not yet email-confirmed
- **WHEN** the user is persisted
- **THEN** the persisted `User.EmailConfirmed` MUST be `false`

### Requirement: Duplicate Email Is Rejected
The system SHALL reject a `POST /api/users` request whose email already corresponds to an existing user by returning HTTP 409 with `ApiResponse<object>.Fail` containing message `"Email already registered"` and MUST NOT create a new user, MUST NOT generate a token, and MUST NOT send any email.

#### Scenario: Existing email returns 409
- **WHEN** `IUserRepository.GetByEmailAsync(request.Email)` returns a non-null user
- **THEN** the controller MUST return HTTP 409 with `ApiResponse<object>.Fail("Email already registered")` and MUST NOT call `IUserRepository.AddAsync`, `IUnitOfWork.SaveChangesAsync`, or `IEmailService.SendEmailAsync`

### Requirement: Confirmation Token Is Generated And Stored
The system SHALL generate a cryptographically random 64-character hex confirmation token, store it on the new user via `User.SetEmailConfirmationToken(token, expiry)`, and set the expiry to `DateTime.UtcNow + 24 hours` so that the existing `POST /api/auth/confirm-email` flow can later validate it.

#### Scenario: Token is set on the created user
- **WHEN** the user is persisted
- **THEN** `User.EmailConfirmationToken` MUST be a non-null, non-empty 64-character hex string and `User.EmailConfirmationTokenExpires` MUST be within 1 minute of `DateTime.UtcNow + 24 hours`

#### Scenario: Token generation uses a cryptographic RNG
- **WHEN** the token is generated
- **THEN** the controller MUST use `System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)` and `Convert.ToHexString` so the token is uniformly distributed and unpredictable

### Requirement: EmailConfirmation Email Is Sent On Creation
The system SHALL send an `EmailConfirmation` email to the new user containing a confirmation link of the form `${Request.Scheme}://${Request.Host}/confirm-email?token={token}` so that the user can complete the email confirmation flow.

#### Scenario: EmailConfirmation email is dispatched
- **WHEN** the user has been persisted
- **THEN** the controller MUST call `IEmailService.SendEmailAsync` exactly once with the user's email, the `EmailConfirmation` template subject, and a body that contains the `ConfirmationLink` variable whose value is `${Request.Scheme}://${Request.Host}/confirm-email?token={token}`

#### Scenario: Welcome email is NOT sent on creation
- **WHEN** the user is created
- **THEN** the controller MUST NOT use the `Welcome` template — the welcome email is dispatched only after the user successfully confirms their email (see the `auth-confirm-email` capability)

### Requirement: Existing Auth Confirm Flow Closes The Loop
The system SHALL rely on the existing `POST /api/auth/confirm-email` endpoint (see `auth-confirm-email` capability) to validate the token, mark `EmailConfirmed = true`, persist the change, and dispatch the `Welcome` email — no additional confirmation logic is added by this capability.

#### Scenario: Confirmation completes the flow
- **WHEN** the user clicks the confirmation link and the frontend posts the token to `POST /api/auth/confirm-email`
- **THEN** the `auth-confirm-email` flow takes over and the user becomes `EmailConfirmed = true`; the `Welcome` email is then sent automatically by `EmailConfirmedEmailHandler`

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

