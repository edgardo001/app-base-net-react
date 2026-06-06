## 1. Verify the implementation is in sync with the new role rule

- [x] 1.1 Confirm `AdminController.SendTestEmail` carries an action-level `[Authorize(Roles = "SuperAdmin,Admin")]` override (commit `cca0088`).
- [x] 1.2 Confirm `AdminController` class-level `[Authorize(Roles = "SuperAdmin")]` is preserved for the other 3 actions (dashboard, audit log, revoke-all-tokens).
- [x] 1.3 Confirm the regression test `AdminControllerTests.SendTestEmail_AuthorizeAttribute_AcceptsAdminAndSuperAdmin` exists and passes (asserts via reflection that the action's `[Authorize]` attribute contains exactly the `SuperAdmin` and `Admin` roles).
- [x] 1.4 Confirm the regression test `AdminControllerTests.AdminController_ClassLevelAuthorize_RestrictsOtherEndpointsToSuperAdmin` exists and passes (documents the asymmetric rule).

## 2. Verify the JWT contract is in sync with the new requirement

- [x] 2.1 Confirm `UserRepository.GetByEmailAsync` includes `UserRoles` → `Role` → `RolePermissions` → `Permission` (commit `34f768b`).
- [x] 2.2 Confirm the regression test `UserConfirmationTokenPersistenceTests.GetByEmailAsync_ReturnsUserWithRolesAndPermissionsLoaded` exists and passes (asserts the navigation properties are populated after a `ChangeTracker.Clear()` and a fresh load).
- [x] 2.3 Confirm the JWT payload structure is documented by `JwtServiceRoleClaimTests.GenerateAccessToken_EmitsRoleClaim_UnderLongUri_NotShortName` (asserts the long URI is the JSON key, not the short `"role"` name).

## 3. Update the spec

- [x] 3.1 Update the `admin-test-email` capability spec: modify the existing `Admin can send a test email` requirement to allow `Admin` OR `SuperAdmin` (replaces the original `SuperAdmin` only).
- [x] 3.2 Add a new requirement `AdminController role asymmetry` documenting the per-action rule and the regression test that guards it.
- [x] 3.3 Add a new requirement `Test-email authorization depends on JWT role claims` documenting the navigation contract for `GetByEmailAsync` and the regression test that guards it.

## 4. Promote and archive

- [x] 4.1 Archive the original `admin-send-test-email` change (promotes the spec to `openspec/specs/admin-test-email/spec.md`).
- [x] 4.2 Create this change (`open-test-email-to-admin-role`) with the modified + added requirements.
- [x] 4.3 Archive this change (merges the modified + added requirements into the global `openspec/specs/admin-test-email/spec.md`).

## 5. Final verification

- [x] 5.1 Run `dotnet test app-base-net-react.slnx --nologo` and confirm 134/134 pass.
- [x] 5.2 Run `cd src/frontend && npm run build` and confirm 0 errors.
- [x] 5.3 Run `openspec list --specs` and confirm `admin-test-email` shows the new requirement count (was 1, should be 3 after the MODIFIED + ADDED merge).
