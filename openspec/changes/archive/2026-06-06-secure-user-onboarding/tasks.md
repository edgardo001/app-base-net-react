## 1. Regla de Oro — Verify baseline

- [x] 1.1 `dotnet test app-base-net-react.slnx --nologo` baseline before the change. Target: 106/106 (69 Application + 37 WebApi).
- [x] 1.2 `cd src/frontend && npm run build` baseline. Target: `built in` clean.

## 2. OpenSpec scaffolding

- [x] 2.1 Create `openspec/changes/secure-user-onboarding/` with `.openspec.yaml`, `proposal.md`, `design.md`, `specs/user-creation/spec.md`, `specs/auth-login/spec.md`, `tasks.md`.

## 3. Application — Port + DTO changes

- [x] 3.1 Add `IRandomPasswordGenerator` to `AppBaseNetReact.Application/Common/Interfaces/IServices.cs` with `string Generate(int length = 12)`.
- [x] 3.2 Remove `Password` from `CreateUserRequest` in `AppBaseNetReact.Application/Common/Validators/UserValidators.cs` (and drop the password rule from `CreateUserRequestValidator`).

## 4. Infrastructure — Adapter + template + notification

- [x] 4.1 Create `AppBaseNetReact.Infrastructure/Services/RandomPasswordGenerator.cs` implementing `IRandomPasswordGenerator`. Uses `RandomNumberGenerator.GetBytes` with rejection sampling for the 62-char charset `[A-Za-z0-9]`, then enforces at least one uppercase, one lowercase, one digit by overwriting random positions.
- [x] 4.2 Register `IRandomPasswordGenerator → RandomPasswordGenerator` in `AppBaseNetReact.WebApi/DependencyInjection.cs` (or wherever the other `I*` ports are registered).
- [x] 4.3 Update `AppBaseNetReact.Infrastructure/Email/Templates/email-confirmation.html` to add a `{{TemporaryPassword}}` block between the existing copy and the CTA button, with Spanish text explaining the password must be changed on first login.
- [x] 4.4 Add `OnboardingEmailResentNotification(UserId, IpAddress, UserAgent)` to `AppBaseNetReact.Application/Features/Users/Notifications/UserNotifications.cs` (or wherever user notifications live).

## 5. Application — Resend command

- [x] 5.1 Create `ResendOnboardingEmailCommand(UserId, IpAddress, UserAgent) : IRequest<ResendOnboardingEmailOutcome>` in `AppBaseNetReact.Application/Features/Users/Commands/ResendOnboardingEmail/ResendOnboardingEmailCommand.cs`.
- [x] 5.2 Create `ResendOnboardingEmailCommandValidator` — `UserId` is a valid Guid (always true; the validator exists to keep the convention).
- [x] 5.3 Create `ResendOnboardingEmailOutcome(ResendOnboardingEmailResult Result)`.
- [x] 5.4 Create `ResendOnboardingEmailResult` in `AppBaseNetReact.Application/Common/Models/` with `ResendErrorCode { None, UserNotFound, AlreadyConfirmed }` and `Success()` / `Fail(code, message)` factories.
- [x] 5.5 Create `ResendOnboardingEmailCommandHandler` that:
  - Looks up `user = await _uow.Users.GetByIdAsync(userId, ct)`. If null → `UserNotFound`.
  - If `user.EmailConfirmed` → `AlreadyConfirmed`.
  - Regenerates the token: `user.SetEmailConfirmationToken(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), _clock.UtcNow.AddHours(24))`.
  - Persists via `_uow.SaveChangesAsync(ct)`.
  - Sends the `EmailConfirmation` email via the existing `IEmailService.SendEmailAsync` with `{{ConfirmationLink}}` populated and **no `{{TemporaryPassword}}`**. Wait — that breaks the template strict-variable rule. See design decision: the resend uses a *different* template (`email-resend.html`) that has only `{{ConfirmationLink}}` and `{{UserName}}`.
  - Publishes `OnboardingEmailResentNotification`.
  - Returns `Success`.

## 6. Infrastructure — Resend template + audit handler

- [x] 6.1 Create `AppBaseNetReact.Infrastructure/Email/Templates/email-resend.html` — a near-clone of `email-confirmation.html` but without the `{{TemporaryPassword}}` block. Mirrors the design decision: the resend contains the link only, not the password.
- [x] 6.2 Add `EmailResend` entry to `Email:Templates` in all 3 appsettings files (example, Production, and the in-test `EmailOptions`).
- [x] 6.3 Create `OnboardingEmailResentAuditLogHandler` writing an `OnboardingEmailResent` audit log via `IAuditService.LogAsync` with `userId`, `ipAddress`, `userAgent`.

## 7. WebApi — Controller

- [x] 7.1 `UsersController` constructor: add `IRandomPasswordGenerator _passwords` parameter.
- [x] 7.2 Rewrite `CreateUser` to:
  - Inject `_passwords` and generate `_passwords.Generate()`.
  - Hash it via `_hasher.HashPassword(password)`.
  - Build user via `User.Create(email, firstName, lastName, hash)`.
  - Call `user.ForcePasswordChange()`.
  - Continue with token + email as today, but pass the plaintext `password` as the `{{TemporaryPassword}}` variable in the `EmailConfirmation` email.
- [x] 7.3 Add `[HttpPost("{id:guid}/resend-onboarding-email")] public async Task<IActionResult> ResendOnboardingEmail(Guid id, CancellationToken ct)`:
  - Build `ResendOnboardingEmailCommand(id, ip, ua)`, send via `_mediator`.
  - Map `ResendErrorCode` to HTTP: `None → 200`, `UserNotFound → 404`, `AlreadyConfirmed → 409`.

## 8. Frontend — Form + Resend button

- [x] 8.1 `src/frontend/src/pages/users.tsx`:
  - Drop the `password` RHF field + the `<Input id="password" type="password" {...register('password')} />` element.
  - Drop the `password` Zod rule from the create schema.
  - Add a "Reenviar" button to each row in the user grid, rendered only when `user.emailConfirmed === false`.
  - Wire the button to call `POST /api/users/{id}/resend-onboarding-email`, show a spinner while in flight, and a success/error toast on completion.
- [x] 8.2 `src/frontend/src/stores/auth-store.ts` — **no change** (the `passwordExpired` field is already wired).
- [x] 8.3 `src/frontend/src/pages/login.tsx` — **no change** (already redirects to `/change-password` when `passwordExpired === true`).
- [x] 8.4 `src/frontend/src/pages/confirm-email.tsx` — **no change** (already redirects to `/login` on success).

## 9. Tests — Backend

- [x] 9.1 Update `UsersControllerTests` to use the new request shape (no `password`). Update the 3 existing tests + add 3 new ones:
  - `CreateUser_GeneratesAndSendsTemporaryPasswordInEmail` — captures the body, asserts it contains the auto-generated password (the same plaintext that was passed to `User.Create`).
  - `ResendOnboardingEmail_WithUnconfirmedUser_RegeneratesTokenAndSendsEmail`.
  - `ResendOnboardingEmail_WithAlreadyConfirmedUser_Returns409`.
- [x] 9.2 Add `ResendOnboardingEmailCommandHandlerTests` with 4 tests (success, user-not-found, already-confirmed, publishes notification with ip+ua).
- [x] 9.3 Add `RandomPasswordGeneratorTests` with 3 tests (length 12, contains all classes, two calls produce different output).
- [x] 9.4 Add `UserConfirmationTokenPersistenceTests.Token_AfterForcePasswordChange_StillAllowsConfirmation` to the existing persistence test file.
- [x] 9.5 Run `dotnet test` and confirm all tests pass. Target: ~117/117 (76 + 41).

## 10. OpenSpec — validate + archive

- [x] 10.1 `openspec validate secure-user-onboarding --strict` → "valid".
- [x] 10.2 `openspec archive secure-user-onboarding -y` → `+ 4 added` to `user-creation` and `+ 1 added` to `auth-login`.

## 11. Final validation

- [x] 11.1 `dotnet build app-base-net-react.slnx --nologo` → 0 errors.
- [x] 11.2 `dotnet test app-base-net-react.slnx --nologo` → all pass.
- [x] 11.3 `cd src/frontend && npm run build` → `built in` clean.
- [x] 11.4 Commit atomically:
  - 1 commit: backend (request, controller, generator, resend command, template, audit handler, tests).
  - 1 commit: frontend (form, resend button).
  - 1 commit: OpenSpec artifacts.
- [x] 11.5 Update `AGENTS.md` migration table if the new Resend CQRS handler changes anything (it doesn't; the new endpoint is fully CQRS already).
