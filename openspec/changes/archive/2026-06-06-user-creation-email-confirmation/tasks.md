## 1. Regla de Oro — Verify baseline

- [x] 1.1 Run `dotnet test app-base-net-react.slnx --nologo` and confirm the empty `UsersControllerTests` file does not break the build. Target: 99/99 (66 Application + 33 WebApi) before adding new tests.

## 2. Baseline tests for UsersController

- [x] 2.1 Create `WebApi.Tests/Controllers/UsersControllerTests.cs` with 2 baseline tests asserting the *current* (buggy) behaviour, before applying the fix. The first test (`CreateUser_WithValidRequest_PersistsAndSendsEmail`) was deliberately lenient so it passes against the pre-fix controller. The second (`CreateUser_WithDuplicateEmail_Returns409`) is a regression guard.
- [x] 2.2 Run `dotnet test` and confirm both new baseline tests pass. Target: 101/101 (66 + 35).

## 3. Fix the controller

- [x] 3.1 Add `using System.Security.Cryptography;` to `UsersController.cs`.
- [x] 3.2 In `CreateUser` (lines ~104-124): after `User.Create(...)` and before `AddAsync`, generate `confirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32))` and call `user.SetEmailConfirmationToken(confirmationToken, DateTime.UtcNow.AddHours(24))`.
- [x] 3.3 Replace the `SendEmail` call to use the `"EmailConfirmation"` template, with variables `{ UserName, ConfirmationLink = $"{Request.Scheme}://{Request.Host}/confirm-email?token={confirmationToken}" }`.
- [x] 3.4 Remove the old `Welcome` template call entirely.

## 4. Tighten the tests

- [x] 4.1 Replace the lenient `CreateUser_WithValidRequest_PersistsAndSendsEmail` with `CreateUser_WithValidRequest_PersistsAndSendsConfirmationEmail`. Capture the user passed to `AddAsync` via `Callback<User, CancellationToken>` and assert `EmailConfirmationToken` is non-empty, `EmailConfirmed == false`, `EmailConfirmationTokenExpires` is within 1 min of `UtcNow + 24h`, and that `SendEmailAsync` is called with subject `"Confirma tu correo"`.
- [x] 4.2 Add `CreateUser_SendsEmailWithConfirmationLink` that captures the email body via `Callback` and asserts it contains the substring `/confirm-email?token=`.
- [x] 4.3 Keep `CreateUser_WithDuplicateEmail_Returns409` as a regression guard.
- [x] 4.4 Run `dotnet test` and confirm all tests pass. Target: 102/102 (66 + 36).

## 5. OpenSpec documentation

- [x] 5.1 Create `openspec/changes/user-creation-email-confirmation/` with `.openspec.yaml`, `proposal.md`, `design.md`, `specs/user-creation/spec.md`, `tasks.md`.
- [x] 5.2 Add 5 requirements under the new `user-creation` capability: persist-with-unconfirmed-email, duplicate-email-rejected, confirmation-token-generated-and-stored, email-confirmation-email-sent-on-creation, existing-auth-confirm-flow-closes-the-loop.
- [ ] 5.3 Run `openspec validate user-creation-email-confirmation --strict` and confirm `"Change 'user-creation-email-confirmation' is valid"`.
- [ ] 5.4 Run `openspec archive user-creation-email-confirmation -y` to sync 5 requirements to `openspec/specs/user-creation/spec.md`. Expect `+ 5 added`.

## 6. Final validation

- [x] 6.1 Run `dotnet build app-base-net-react.slnx --nologo` — 0 errors, 0 warnings.
- [x] 6.2 Run `dotnet test app-base-net-react.slnx --nologo` — 102/102 pass.
- [x] 6.3 Commit atomically: `fix(users): send email confirmation link (not welcome) on user creation`.
