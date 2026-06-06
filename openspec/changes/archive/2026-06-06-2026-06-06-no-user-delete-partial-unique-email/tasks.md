## 1. Tests that document the gap (red light)

- [x] 1.1 `UserConfirmationTokenPersistenceTests.GetByEmailAsync_ForSoftDeletedUser_ReturnsNull` — asserts the soft-delete invariant.
- [x] 1.2 `UserConfirmationTokenPersistenceTests.CreateUser_PassesPreCheck_WhenOnlySoftDeletedUserHasEmail` — asserts the controller-layer pre-check gap.
- [x] 1.3 `UsersControllerTests.CreateUser_WhenOnlySoftDeletedUserHasEmail_ProceedsWithInsert` — same gap from the controller side.
- [x] 1.4 4 new `OnboardingEmailResent{Audit,Email}Handler` tests (gap detected during the test sweep).

## 2. Remove the DeleteUser route

- [x] 2.1 Remove `[HttpDelete("{id:guid}")] DeleteUser(Guid id, CancellationToken ct)` action from `UsersController.cs`.
- [x] 2.2 Remove `Trash2` import, `deleteUser` function, and the trash `<Button>` from `pages/users.tsx`.

## 3. Database: partial unique indexes

- [x] 3.1 Update `UserConfiguration` in `EntityConfigurations.cs` to add `HasFilter("\"DeletedAt\" IS NULL")` to both `IX_Users_Email` and `IX_Users_NormalizedEmail`.
- [x] 3.2 Add EF Core migration `20260606143024_MakeEmailAndNormalizedEmailPartialUniqueIndexes` (hand-written because EF Core emits an empty diff for filter-only changes).
- [x] 3.3 Update `AppDbContextModelSnapshot.cs` to reflect the new filter.
- [x] 3.4 Verify generated SQL with `dotnet ef migrations script --idempotent` (PostgreSQL `WHERE "DeletedAt" IS NULL` clause present).

## 4. CS8625 fallout from removing the route

- [x] 4.1 `ApiResponse<T>.Ok(string message)` overload added; 17 `Ok(null, "msg")` call-sites in 5 controllers migrated to the new overload. 0 warnings.

## 5. Verification

- [x] 5.1 `dotnet build` — 0 warnings, 0 errors.
- [x] 5.2 `dotnet test` — 128/128 pass.
- [x] 5.3 `npm run build` — frontend builds.
