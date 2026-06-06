## Why

The current `CreateUser` flow requires the admin to type a password
on behalf of the new user. The new user then receives a generic
`EmailConfirmation` link with no credential. This is both insecure
(admin might pick a weak password, password is sent through a
channel the admin controls, not the user) and incomplete (if the
email is lost, the user is stranded with no way back in until an
admin manually intervenes).

This change reshapes the flow around an **admin-free onboarding**:

1. The admin only enters the user's email, first name, last name,
   and roles. No password.
2. The system generates a cryptographically random 12-character
   password.
3. The user is created with the password hash and `LastPasswordChangeAt = null`
   (so the next login is forced to change it).
4. The `EmailConfirmation` email is sent with both the confirmation
   link AND the auto-generated password, so the user can log in
   with it.
5. The user clicks the link → email is confirmed → redirected to
   `/login` → logs in with the auto-generated password → the
   `LoginResponse.passwordExpired = true` flag (already surfaced by
   `User.IsPasswordExpired()` when `LastPasswordChangeAt == null`)
   causes the frontend to redirect to `/change-password`.
6. The user picks a real password. The change updates
   `LastPasswordChangeAt = UtcNow`, so the flag goes back to false
   on subsequent logins.

The change also adds a **resend onboarding email** endpoint so the
admin can recover when a user says "I never got the email" or
"I deleted it by mistake". The endpoint only works for users who
have not yet confirmed their email — once confirmed, the link is
moot and the password has been replaced by the user, so there is
nothing useful to resend. The resend contains the same content as
the initial email (link + auto-generated password), so a user who
lost both pieces can recover fully without admin intervention
beyond the click.

## What Changes

### Backend

- **`CreateUserRequest`**: removes the `Password` field. The request
  is now `(Email, FirstName, LastName, RoleIds?)`.
- **`CreateUserRequestValidator`**: drops the password rule. Email,
  first name, last name validation is unchanged.
- **`IRandomPasswordGenerator`** (new port in
  `Application/Common/Interfaces/IServices.cs`): generates a
  cryptographically random 12-character password from the charset
  `[A-Za-z0-9]` with at least one uppercase, one lowercase, and one
  digit. Implementation `RandomPasswordGenerator` in
  `Infrastructure/Services/`.
- **`UsersController.CreateUser`**:
  - Calls `_passwords.Generate()` to produce a temporary password.
  - Hashes it via the existing `IPasswordHasherService`.
  - Creates the `User` via `User.Create(email, firstName, lastName, hashed)`
    exactly as today, then calls `user.ForcePasswordChange()` so
    `LastPasswordChangeAt == null` and the next login is forced
    to change.
  - Generates the confirmation token (as today).
  - Sends the `EmailConfirmation` email with **two variables**:
    `{{ConfirmationLink}}` (today) and the new
    `{{TemporaryPassword}}` (auto-generated, plain text).
- **`email-confirmation.html`** template: adds a styled
  `<code>{{TemporaryPassword}}</code>` block under the existing
  copy, with a sentence in Spanish explaining that the user must
  change this password on first login.
- **Resend command** (`Application/Features/Users/Commands/ResendOnboardingEmail/`):
  `ResendOnboardingEmailCommand(UserId, IpAddress, UserAgent) : IRequest<ResendOnboardingEmailOutcome>`,
  validator, outcome, `ResendOnboardingEmailResult` with
  `ResendErrorCode { None, UserNotFound, AlreadyConfirmed }`, and
  `ResendOnboardingEmailCommandHandler` that:
  - Looks up the user by `UserId`.
  - Returns `UserNotFound` if missing.
  - Returns `AlreadyConfirmed` if `user.EmailConfirmed` is true
    (no recovery content to resend).
  - Regenerates the confirmation token (via
    `user.SetEmailConfirmationToken(...)`) so the previous link is
    invalidated.
  - Persists via `IUnitOfWork.SaveChangesAsync`.
  - Sends the same `EmailConfirmation` email as `CreateUser`
    (with link + the same `TemporaryPassword` that was originally
    generated; the plaintext is re-read from the existing
    `Notification`-style flow — see design notes).
  - Publishes `OnboardingEmailResentNotification` for the audit
    log.
- **`UsersController.ResendOnboardingEmail(Guid id)`**:
  `[HttpPost("{id:guid}/resend-onboarding-email")]` →
  `_mediator.Send(new ResendOnboardingEmailCommand(id, ip, ua))` →
  map `ResendErrorCode` to HTTP (`None` → 200 with a
  user-friendly message, `UserNotFound` → 404, `AlreadyConfirmed` →
  409 with a message explaining there is nothing to resend).
- **`IEmailService`**: no changes — the password is a
  `Dictionary<string, string>` variable, the same way
  `ConfirmationLink` is passed today.
- **Audit log**: `OnboardingEmailResentAuditLogHandler` writes an
  `OnboardingEmailResent` action entry with `userId`, `ipAddress`,
  `userAgent`.
- **`UserRepository`** (Infrastructure): no schema changes. The
  resend mutates the existing `EmailConfirmationToken` /
  `EmailConfirmationTokenExpires` columns.

### Frontend

- **`src/frontend/src/pages/users.tsx`**:
  - Removes the password input + RHF field + validation from the
    create form. The form becomes `(email, firstName, lastName,
    roleIds?)`.
  - Adds a "Reenviar" button to each row in the user grid **only
    when `!user.emailConfirmed`**. Clicking it POSTs to
    `/users/{id}/resend-onboarding-email` and shows a success/error
    toast. While the request is in flight, the button shows a
    spinner and is disabled.
- **`src/frontend/src/stores/auth-store.ts`**: **no changes** — the
  `passwordExpired` field is already there and the login response
  already includes it.
- **`src/frontend/src/pages/login.tsx`**: **no changes** — it
  already redirects to `/change-password` when `passwordExpired`
  is true.
- **`src/frontend/src/pages/confirm-email.tsx`**: **no changes** —
  it already redirects to `/login` on success.
- **`src/frontend/src/pages/change-password.tsx`**: **no changes**
  — the existing page works for both "expired" and "forced" cases.

### OpenSpec

- `user-creation` capability grows by 3 new requirements (no admin
  password, includes temp password, resend endpoint).
- `auth-login` capability grows by 1 new requirement
  (LoginResponse surfaces `passwordExpired`, already implemented
  but now formally specified).

## Capabilities

### New Capabilities

- (none — extending existing)

### Modified Capabilities

- `user-creation` — 3 requirements added.
- `auth-login` — 1 requirement added.

## Impact

- **Code (backend)**:
  - `AppBaseNetReact.Application/Common/Validators/UserValidators.cs`
    — drop password rule + drop field from `CreateUserRequest`.
  - `AppBaseNetReact.Application/Common/Interfaces/IServices.cs`
    — add `IRandomPasswordGenerator`.
  - `AppBaseNetReact.Application/Features/Users/Commands/ResendOnboardingEmail/`
    — 4 new files (Command, Validator, Outcome, Handler).
  - `AppBaseNetReact.Application/Features/Users/Notifications/UserNotifications.cs`
    — 1 new record (`OnboardingEmailResentNotification`).
  - `AppBaseNetReact.Application/Common/Models/ResendOnboardingEmailResult.cs`
    — 1 new file.
  - `AppBaseNetReact.Infrastructure/Services/RandomPasswordGenerator.cs`
    — 1 new file.
  - `AppBaseNetReact.Infrastructure/Notifications/OnboardingEmailResentAuditLogHandler.cs`
    — 1 new file.
  - `AppBaseNetReact.WebApi/Controllers/UsersController.cs` —
    rewrite `CreateUser` to use the generator and add
    `ResendOnboardingEmail` action.
  - `AppBaseNetReact.Infrastructure/Email/Templates/email-confirmation.html`
    — add the temporary-password block.
  - `AppBaseNetReact.Application/Common/Models/LoginResult.cs` (or
    wherever `LoginResponse` lives) — no code change, the existing
    `PasswordExpired` field is already wired. Spec is updated.
  - `AppBaseNetReact.WebApi/DependencyInjection.cs` — register
    `IRandomPasswordGenerator` → `RandomPasswordGenerator`.
- **Code (frontend)**:
  - `src/frontend/src/pages/users.tsx` — drop password field, add
    Resend action.
- **HTTP contract**:
  - `POST /api/users` request shape changes (loses `password`).
    Any client that sent `password` will now get a 400 from
    FluentValidation (the field is ignored on the server, but the
    client may behave unexpectedly if it relied on a confirmation).
  - `POST /api/users/{id}/resend-onboarding-email` is a new
    endpoint.
  - `POST /api/auth/login` response is unchanged.
  - `POST /api/users` response is unchanged.
- **Database**: no schema changes.
- **Dependencies**: no new NuGet or npm packages.
- **Configuration**: no env-var changes.
- **Docker**: no changes.
- **Backwards compatibility**: breaking for any external client
  that depended on the admin-supplied password. None known.
