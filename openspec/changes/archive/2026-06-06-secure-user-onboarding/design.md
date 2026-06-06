## Context

The original flow has the admin supply the password. This was the
path of least resistance during initial scaffolding, but it has
two problems:

1. The admin picks a password on behalf of the user. If the admin
   picks a weak one (or worse, re-uses a known one), the user
   inherits that weakness. The admin also sees the password,
   which they should not.

2. There is no recovery path if the user never receives the
   confirmation email. The user has no password, no confirmed
   email, and the only way out is the admin manually deleting and
   re-creating them.

The new flow inverts ownership: the **system** generates the
password, the user receives it (along with the link) in a single
email, and the **force-change-on-first-login** invariant (already
implemented via `User.IsPasswordExpired()` returning true when
`LastPasswordChangeAt == null`) ensures the password is replaced
with one the user picks before any sensitive operation runs.

## Architecture compliance

- **Domain**: `User.Create(...)` and `User.ForcePasswordChange()`
  are unchanged. The flow already supports
  `LastPasswordChangeAt == null` as the "must change" signal
  (used today by the seeder for `admin/admin` and by
  `ResetPasswordCommandHandler` after a reset).
- **Application**: new port `IRandomPasswordGenerator`. New
  command/handler under `Features/Users/Commands/ResendOnboardingEmail/`.
  Follows the same `IRequest<Outcome>` + typed `*Result` pattern
  used by every other auth/user command. `ValidationBehavior`
  (FluentValidation) runs the new validator automatically.
- **Infrastructure**: new adapter `RandomPasswordGenerator`
  (uses `RandomNumberGenerator.GetBytes`). New
  `OnboardingEmailResentAuditLogHandler`. No schema changes, no
  new NuGet packages.
- **WebApi**: `UsersController` is controller-orchestrated for
  `CreateUser` already (per the migration table in `AGENTS.md`);
  this change does not migrate it to CQRS. The new
  `ResendOnboardingEmail` action IS implemented as a CQRS
  command/handler dispatched via `_mediator.Send(...)`, matching
  the pattern of the other auth endpoints. (This is a deliberate
  inconsistency with `CreateUser` because the resend is small
  enough that a full migration would just be churn; it is tracked
  in the AGENTS.md follow-up note.)
- **Frontend**: minimal changes — drop one form field, add one
  button + one toast.

## Password generation algorithm

```
12 characters from [A-Za-z0-9] (62 chars)
log2(62) ≈ 5.95 bits/char
12 chars ≈ 71 bits of entropy
```

Algorithm (in `RandomPasswordGenerator.Generate(int length = 12)`):

1. Fill the 12 positions with uniform random chars from
   `[A-Za-z0-9]` using `RandomNumberGenerator.GetBytes` and a
   rejection-sampling loop (mod 62 with re-roll on values ≥ 62 to
   avoid bias).
2. **Enforce at least one uppercase, one lowercase, one digit**:
   generate three extra bytes, take each modulo the appropriate
   set, and overwrite random positions in the buffer until the
   constraint is met. This keeps the password uniformly random
   *modulo* the policy (the policy bits are not added to the
   entropy, they just narrow the charset), and is well below the
   password-policy requirement.
3. Return the buffer as a `string`.

Why 12 chars and not 16 or 8?
- 8 chars × 5.95 bits ≈ 47 bits — borderline by 2026 standards
  for an account that might be reused across services.
- 12 chars × 5.95 bits ≈ 71 bits — comfortably above NIST
  SP 800-63B's 64-bit recommendation for memorable secrets.
- 16 chars adds no security given the forced change on first
  login, and degrades UX (typing a longer password from an email
  on a phone).

The password is single-use: the user is forced to change it on
first login, so the only window during which the 12-char password
is valid is from "email received" to "change-password form
submitted". Brute-force within that window is impossible (the
endpoint has rate limiting and the window is minutes, not days).

## Token regeneration on resend

The resend handler calls `user.SetEmailConfirmationToken(...)`
with a fresh 32-byte hex + 24h expiry, then `SaveChangesAsync`.
This **invalidates the previous token**: any link the user might
have had from the first email becomes dead. This is intentional —
we don't want a stale link floating around after a resend.

The plaintext `TemporaryPassword` is NOT regenerated on resend.
The auto-generated password was already hashed and stored at
creation time, and the resend must include the same password the
user originally received. Concretely:

- At `CreateUser` time, the controller generates a password,
  hashes it for the DB, and **publishes a notification** with
  the plaintext password.
- A handler (`OnboardingEmailDispatchHandler`) listens to that
  notification and sends the email.
- On resend, the handler does NOT regenerate the password; it
  just re-sends the same plaintext. Since the plaintext is no
  longer in memory after the original send, the resend handler
  cannot include it.

This is a real design constraint. The two viable solutions are:

**Option A — Re-generate the password on resend.**
The user's original password stops working, which is worse than
no resend at all. Rejected.

**Option B — Persist the plaintext password temporarily.**
Encrypt it with a server-side key, store it in a separate column
with a short TTL, and let the resend handler read+decrypt it.
Secure but adds a column, an encryption key, a TTL sweeper, and
new failure modes.

**Option C — Accept that resend only contains the new link, and
the user has to use the original password (or hit
"ForgotPassword" if they lost it).**
The admin's UX is: "I resent the email", the user's UX is:
"I have the link, I have the password from the first email". If
the user lost both, they use the existing `ForgotPassword` flow
to set a new one (and that path is well-tested).

**Decision: Option C.** The resend is explicitly "send the link
again", not "send everything again". The proposal text was
ambiguous about this; the design is: resend contains the new
confirmation link only. If the user lost both the link AND the
password, they use `POST /api/auth/forgot-password` to recover
the password (which is the canonical recovery path in this
application — see the `auth-forgot-password` capability).

**Action: the proposal is updated to reflect this.** The spec
requirement "EmailConfirmation Email Includes Temporary Password"
is preserved for the **initial** email; the resend contains the
link only and is documented in its own requirement.

## Resend endpoint authorization

`POST /api/users/{id}/resend-onboarding-email` is an admin
operation. Today `POST /api/users` is admin-only (the existing
`CreateUser` is protected by the controller-level
`[Authorize]` on `UsersController` and the user-grid
callsite is the admin "Users" page).

The new action is added to the same controller, so it inherits
the same authorization. The capability spec does not enumerate
permissions because they are an orthogonal concern handled by
the existing `[Authorize]` + `HasPermission` middleware.

## Email template changes

The new `{{TemporaryPassword}}` variable is rendered in a
dedicated `<code>` block with a contrasting background, sitting
between the existing copy ("confirma tu dirección de correo…")
and the CTA button. The text immediately above the block says
"Tu contraseña temporal es:" and immediately below says
"Deberás cambiar esta contraseña al iniciar sesión."

The template still works correctly without the variable being
provided (the `EmailRenderer` would throw `Missing variable`
today; this is acceptable because every call site must provide
both variables together). A small follow-up could add a
`strict` mode to the renderer that allows optional variables.

## Frontend resend UX

The user grid (`users.tsx`) already has a "Reenviar" action
implicit in the spec ("El panel de usuario indica si el usuario
confirmó su correo"). This change makes it real:

- Each row's action cell gets a "Reenviar correo" button when
  `user.emailConfirmed === false`.
- The button is disabled while the request is in flight (shows
  a spinner).
- On 200, a success toast: "Correo reenviado a <email>".
- On 409 (`AlreadyConfirmed`), a warning toast: "El usuario ya
  confirmó su correo".
- On 404, an error toast: "Usuario no encontrado".
- On any other error, a generic error toast.

The resend does not require admin re-confirmation because it is
already gated by the admin-only controller and the 409 response
on already-confirmed users makes accidental resends a no-op.

## Test coverage

### Backend (xUnit)

- `UsersControllerTests` — update existing tests for new request
  shape (no `Password`); add 2 new tests:
  - `CreateUser_GeneratesAndSendsTemporaryPasswordInEmail`
  - `ResendOnboardingEmail_RegeneratesTokenAndSendsEmail`
  - `ResendOnboardingEmail_WhenAlreadyConfirmed_Returns409`
- `ResendOnboardingEmailCommandHandlerTests` (new, ~4 tests):
  - `WithExistingUnconfirmedUser_RegeneratesTokenAndSendsEmail`
  - `WithUnknownUser_ReturnsUserNotFound`
  - `WithConfirmedUser_ReturnsAlreadyConfirmed`
  - `PublishesOnboardingEmailResentNotificationWithIpAndUserAgent`
- `RandomPasswordGeneratorTests` (new, ~3 tests):
  - `GeneratesRequestedLength`
  - `ContainsAllRequiredCharacterClasses`
  - `IsCryptographicallyRandom_NoTwoConsecutiveCallsAreEqual`
- `UserConfirmationTokenPersistenceTests` (existing) — add:
  - `Token_AfterForcePasswordChange_StillAllowsConfirmation` (the
    new flow calls `ForcePasswordChange` after `SetEmailConfirmationToken`;
    the token must still be findable afterwards).

### Frontend

- No automated tests (no Vitest infrastructure for components
  in this repo). Manual verification: `npm run build` succeeds
  and the new form/button render.

## Out of scope

- Migrating `CreateUser` to a `CreateUserCommand` handler
  (still controller-orchestrated; the AGENTS.md follow-up note
  covers this).
- Multi-channel delivery (SMS, push) for the onboarding email.
- Re-encrypting the temporary password in a separate column
  (see Option B above; rejected in favour of the simpler
  "resend the link only" semantics).
- Per-tenant or per-role onboarding email templates.
- A "preview" of the generated password in the admin UI (the
  admin has no business seeing the password — that's the whole
  point of this change).
- Configuration of password length / charset (12-char alphanumeric
  is hard-coded; can be lifted to `EmailOptions` later if needed).
- A bulk-resend endpoint for "resend to all unconfirmed users
  whose email was bounced by the SMTP server".
