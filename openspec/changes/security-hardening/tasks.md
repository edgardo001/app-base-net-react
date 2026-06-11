## 1. Baseline — Regla de Oro (tests against the existing code)

- [x] 1.1 Run `dotnet test AppBaseNetReact.slnx` — confirm 247/247 green before any production code changes
- [x] 1.2 Verify `PasswordPolicyService.PasswordHistoryCount = 5` exists but is not enforced in `ChangePasswordCommandHandler`
- [x] 1.3 Verify no CSRF middleware exists in `Program.cs` middleware pipeline

## 2. Domain — PasswordHistory entity

- [x] 2.1 Create `Domain/Entities/PasswordHistory.cs` with properties: `Id`, `UserId`, `PasswordHash`, `CreatedAt`
- [x] 2.2 Add `PasswordHistories` navigation to `User` entity: `ICollection<PasswordHistory>`
- [x] 2.3 Create `Infrastructure/Persistence/Configurations/PasswordHistoryConfiguration.cs` with FK to User, index on (UserId, CreatedAt)
- [x] 2.4 Add `DbSet<PasswordHistory> PasswordHistories` to `AppDbContext`
- [x] 2.5 Create EF migration for the new table
- [x] 2.6 Run `dotnet build` — confirm 0 errors

## 3. Application — PasswordHistory domain logic

- [x] 3.1 Add `CheckPasswordHistoryAsync(Guid userId, string newPassword, CancellationToken ct)` to `IPasswordPolicyService` interface
- [x] 3.2 Implement in `PasswordPolicyService`: load last N hashes, run `_hasher.Verify` against each, return error if match found
- [x] 3.3 Update `ChangePasswordCommandHandler` to call `CheckPasswordHistoryAsync` before setting new hash
- [x] 3.4 After successful password change, store new hash via `IPasswordHistoryRepository.Add` and trim to `PasswordHistoryCount`
- [x] 3.5 Add `IPasswordHistoryRepository` to `IUnitOfWork` (or use existing pattern)
- [x] 3.6 Create `Infrastructure/Persistence/Repositories/PasswordHistoryRepository.cs`

## 4. Tests — PasswordHistory backend

- [x] 4.1 Create `Application.Tests/Features/Auth/Commands/ChangePassword/ChangePasswordCommandHandlerTests.cs` — add tests: password matches history (rejected), password does not match history (accepted + stored), history cleanup after exceeding limit
- [x] 4.2 Create `Application.Tests/Services/PasswordPolicyServiceTests.cs` — add test: `CheckPasswordHistoryAsync` with matching hash returns error
- [x] 4.3 Create `Application.Tests/Domain/PasswordHistoryTests.cs` — basic entity creation test
- [x] 4.4 Run `dotnet test` — confirm all new tests pass

## 5. CSRF — Backend middleware

- [x] 5.1 Create `WebApi/Middleware/CsrfMiddleware.cs` — validates `X-CSRF-TOKEN` header on POST/PUT/PATCH/DELETE; excludes `/api/auth/login`, `/api/auth/logout`, `/api/auth/refresh`, `/api/auth/forgot-password`, `/api/auth/reset-password`, `/api/auth/confirm-email`, `/health/*`
- [x] 5.2 Register middleware in `Program.cs` pipeline (after CORS, before Authentication)
- [x] 5.3 Create `WebApi.Tests/Middleware/CsrfMiddlewareTests.cs` with tests: missing header → 403, valid header → passes, excluded route → no validation, GET request → no validation
- [x] 5.4 Run `dotnet test` — confirm all CSRF tests pass

## 6. CSRF — Frontend

- [x] 6.1 Update `src/frontend/src/lib/api.ts` (axios instance) — add request interceptor that sets `X-CSRF-TOKEN` header on POST/PUT/PATCH/DELETE with a random UUID generated at app init
- [x] 6.2 Verify GET/OPTIONS requests are not modified
- [x] 6.3 Run `cd src/frontend && npm run build` — confirm clean build

## 7. Final validation

- [x] 7.1 Run `dotnet build` — 0 errors
- [x] 7.2 Run `dotnet test` — 392/392 pass
- [x] 7.3 Run `npm run build` — clean build
