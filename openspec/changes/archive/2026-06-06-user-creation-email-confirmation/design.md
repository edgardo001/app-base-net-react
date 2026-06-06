## Context

`UsersController.CreateUser` is the only endpoint in the users domain
and is the second non-auth endpoint in the project (after
`AuthController.ConfirmEmail`) that crosses the application boundary.

It is intentionally **not** migrated to MediatR in this change. The
fix is a minimal, surgical correction: replace the wrong email
template with the right one, and set up the persistence invariants
the existing `ConfirmEmail` flow already expects. Migrating
`CreateUser` to a `CreateUserCommand` + handler is a separate,
larger refactor (would need to extract the role-resolution logic,
audit logging, and password-hashing dependencies out of the
controller) and is out of scope for a bug fix.

## Architecture compliance

- **Domain**: zero new logic. The change uses two existing methods on
  `User`: `User.Create(...)` (existing) and
  `User.SetEmailConfirmationToken(token, expires)` (added in
  `cqrs-auth-confirm-email`).
- **Application**: untouched. `IUserRepository.GetByEmailAsync` is
  the only port invoked, and it is unchanged.
- **Infrastructure**: untouched. `IEmailService.SendEmailAsync` is
  the same port that `ConfirmEmail` uses; the `EmailConfirmation`
  template config already exists.
- **WebApi**: minimal change. `UsersController` gains a
  `System.Security.Cryptography` using and 3 lines (token gen +
  `SetEmailConfirmationToken` call) and swaps one `SendEmail` call's
  template name + variables.

## Token generation

- 32 bytes from `RandomNumberGenerator.GetBytes(32)`, encoded as
  uppercase hex via `Convert.ToHexString` → 64-char string.
- Mirrors the `ConfirmEmail` flow's expectations:
  `IUserRepository.GetByEmailConfirmationTokenAsync` (added in
  `cqrs-auth-confirm-email`) is a `WHERE EmailConfirmationToken = @p`
  query — exact string match. The hex encoding is case-stable, so
  this works regardless of DB collation.
- Expiry: 24h from creation, stored as `DateTime.UtcNow` on the
  `User` entity (which converts to UTC at the DB boundary in
  PostgreSQL).

## Why not send a "magic" Welcome email that auto-confirms?

Because the spec says users must confirm their own email
(`auth-confirm-email` capability). Auto-confirming would bypass the
audit log, the `EmailConfirmed` flag, and the rate-limiting
opportunity the confirmation flow provides.

## Why a fixed 24h expiry?

- Aligns with the 24h expiry `ConfirmEmailCommandHandler` already
  validates against.
- Short enough that abandoned users self-clean.
- Long enough for a user to find the email in their inbox and click
  the link.
- Configurability is deferred: a `ConfirmationTokenLifetimeHours`
  setting can be added later via `IOptions<>` if needed.

## Test coverage

3 new tests in `UsersControllerTests` (the file was previously empty
— `UsersController` had **zero** test coverage before this change):

1. `CreateUser_WithValidRequest_PersistsAndSendsConfirmationEmail` —
   captures the user passed to `AddAsync`, asserts
   `EmailConfirmationToken` is set,
   `EmailConfirmed` is `false`,
   `EmailConfirmationTokenExpires` is within 1 min of
   `UtcNow + 24h`, and that `SendEmailAsync` is called once with
   the `EmailConfirmation` subject.
2. `CreateUser_SendsEmailWithConfirmationLink` — captures the email
   body, asserts it contains the substring
   `/confirm-email?token=`.
3. `CreateUser_WithDuplicateEmail_Returns409` — regression guard for
   the existing email-uniqueness check (was previously untested).

The first two tests are mutually reinforcing: test #1 verifies the
persistence invariant, test #2 verifies the email content. Both
were impossible to write before the fix because the controller was
not generating a token at all.

## Out of scope

- Migrating `CreateUser` to a `CreateUserCommand` + handler
  (separate change, would need its own proposal).
- Frontend `/confirm-email` page (separate change, frontend domain).
- Configurable token lifetime.
- Rate-limiting `POST /api/users` (admin-only; not a public attack
  surface).
- Updating the `DatabaseSeeder` to use a real-looking email
  (`admin@admin.local`) for new installs. Currently
  `DatabaseSeeder.cs:104` uses `Email = "admin"` so the admin can
  log in before the new confirmation flow requires email
  confirmation. The seeder sets `EmailConfirmed = true`
  explicitly (line 120), so the seeder is internally consistent.
