## Why

`UsersController.CreateUser` (the only non-auth controller endpoint that
mutates state) was creating users with `EmailConfirmed = false` and then
sending the `Welcome` template — a plain greeting with no link. There was
no path from "user created" to "user confirms their email" because no
confirmation token was ever generated and the welcome email had nothing
to click.

Consequence: the seeded `admin` user could log in only because the
seeder explicitly sets `EmailConfirmed = true` (`DatabaseSeeder.cs:120`).
Any new user created via `POST /api/users` could never confirm their
email, and login returned `EmailNotConfirmed` (403) — the
`ConfirmEmailCommandHandler` (shipped in `cqrs-auth-confirm-email`) had
nothing to look up.

This change records the bug fix as a spec-level requirement so the
behaviour does not regress and so the new end-to-end confirmation flow
is documented alongside the other auth capabilities.

## What Changes

- `UsersController.CreateUser` now generates a 32-byte cryptographically
  random confirmation token, sets it on the user via
  `User.SetEmailConfirmationToken(token, UtcNow + 24h)`, persists, and
  sends the `EmailConfirmation` template (which already supports
  `{{ConfirmationLink}}`) instead of `Welcome`.
- The confirmation link points to
  `${Request.Scheme}://${Request.Host}/confirm-email?token={token}` and
  is delivered in the email body as the `ConfirmationLink` variable.
- The existing `POST /api/auth/confirm-email` flow
  (`cqrs-auth-confirm-email`) takes over after the user clicks: it
  validates the token, marks `EmailConfirmed = true`, and the
  `EmailConfirmedEmailHandler` automatically sends the `Welcome` email.
- New `UserCreationResult` / `UserErrorCode` are not introduced: the
  controller already returns 201 / 409 directly. This change only
  tightens the side-effects (token + email) and does not add new public
  surface.
- 3 new baseline tests in `UsersControllerTests` (the file was empty
  before this change; this is the first test coverage for
  `UsersController`).

## End-to-end flow after this change

1. Admin calls `POST /api/users` with `{ email, firstName, lastName, password, roleIds? }`.
2. Handler creates the user, sets `EmailConfirmationToken` + 24h expiry,
   persists, and sends the `EmailConfirmation` email.
3. User receives the email, clicks the link → browser hits
   `/confirm-email?token=...`.
4. Frontend posts to `POST /api/auth/confirm-email` with `{ token }`.
5. `ConfirmEmailCommandHandler` validates the token, calls
   `user.ConfirmEmail()`, persists, publishes `EmailConfirmedNotification`.
6. `EmailConfirmedEmailHandler` sends the `Welcome` email automatically.
7. User calls `POST /api/auth/login` and is authenticated.

## Capabilities

### New Capabilities

- `user-creation`: An admin creates a new user. The user is persisted
  with `EmailConfirmed = false`, receives a single-use confirmation
  token with a 24-hour expiry, and is sent an `EmailConfirmation` email
  containing a link to complete the flow.

### Modified Capabilities

- (none — `auth-confirm-email` already specifies the confirmation side
  of the flow and is unchanged)

## Impact

- **Code**:
  - `src/backend/AppBaseNetReact.WebApi/Controllers/UsersController.cs`
    (added `System.Security.Cryptography` using; rewrote
    `CreateUser` to generate token + send `EmailConfirmation`)
  - `src/backend/AppBaseNetReact.WebApi.Tests/Controllers/UsersControllerTests.cs`
    (new file, 3 tests)
- **HTTP contract**: unchanged on the happy path (still 201 Created with
  `{ id, email }`); the email that is sent is now `EmailConfirmation`
  instead of `Welcome` (the seed admin's `DatabaseSeeder` flow is
  unchanged).
- **Database**: no schema changes. `EmailConfirmationToken` and
  `EmailConfirmationTokenExpires` columns already exist (added in
  `cqrs-auth-confirm-email`).
- **Dependencies**: no new NuGet packages.
- **Configuration**: `Email:Templates:EmailConfirmation` already exists
  in all `appsettings*.json` variants — no env-var changes.
- **Docker**: no changes.
- **Frontend**: should already work with the new flow; if the
  `/confirm-email` page is not yet implemented, this fix unblocks the
  build but the link target will 404 until the page is added.
