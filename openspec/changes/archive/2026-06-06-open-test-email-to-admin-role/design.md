## Context

The `admin-test-email` capability was added in `openspec/changes/admin-send-test-email/` (archived to `2026-06-06-admin-send-test-email/`) and shipped with a single requirement restricting the `POST /api/admin/test-email` endpoint to the `SuperAdmin` role. After deployment, two things changed in the code without a corresponding spec update:

1. **The role rule was relaxed.** Commit `cca0088` added an action-level `[Authorize(Roles = "SuperAdmin,Admin")]` override on `AdminController.SendTestEmail`, opening the endpoint to the `Admin` role. The implementation was a one-character-of-typing fix: the spec said `SuperAdmin`, the code now says `SuperAdmin,Admin`. The asymmetry (test-email is more permissive than the other 3 admin endpoints) was intentional and reflected a real operational need (Admins need to verify SMTP without waiting on a SuperAdmin), but it was a silent code change.

2. **A related production bug surfaced.** When the user reported that the test-email card was hidden in the frontend even for a user with the `Admin` role, debugging revealed that `UserRepository.GetByEmailAsync` had no `Include` for the `UserRoles` navigation. The `LoginCommandHandler` used that method to load the user for JWT generation; with `user.UserRoles` empty, the JWT was issued with zero role claims and zero permission claims, and every `[Authorize(Roles = "...")]` check on the backend returned 403 for every user. Commit `34f768b` added the missing `Include(UserRoles).ThenInclude(Role).ThenInclude(RolePermissions).ThenInclude(Permission)` chain and a regression test that uses `ChangeTracker.Clear()` to force a fresh load (the only way the test catches a missing `Include` against the InMemory provider, which returns tracked entities otherwise).

The new requirements in this change close the documentation loop for both findings. The code changes are already in place; this is a spec sync, not a code change.

## Goals / Non-Goals

**Goals:**
- Document the new role rule (`SuperAdmin` OR `Admin`) for the test-email endpoint.
- Document the role asymmetry as a first-class contract: test-email is the only `AdminController` action that accepts the `Admin` role.
- Document the JWT contract dependency on `GetByEmailAsync` deep-loading navigations, so that future developers who add a caller of `GetByEmailAsync` (or who change the `LoginCommandHandler`) understand the navigation contract that the JWT writer relies on.
- Capture the regression tests that guard these contracts, so the spec is tied to a runnable artifact.

**Non-Goals:**
- No new code change. The implementation is correct.
- No new API surface. The endpoint contract is unchanged.
- No role rule changes for the other `AdminController` actions (dashboard, audit log, token revocation) — they remain `SuperAdmin` only at the class level.
- No rate-limiting change for the test-email endpoint. Admin-only, low volume, already documented as a non-goal in the original design.

## Decisions

1. **Spec sync via a new change, not an edit to the archived spec** → A new OpenSpec change is the correct workflow for syncing the spec with the implementation. Editing the archived `2026-06-06-admin-send-test-email` directly would break the OpenSpec invariant that specs are immutable once archived. Alternatives considered: rolling the spec change into the same change that introduced the new behavior (not possible, the code change is already shipped); reopening the archived change (not supported by the CLI).

2. **`## MODIFIED Requirements` for the existing requirement + `## ADDED Requirements` for the new ones** → The existing requirement's role rule changes from `SuperAdmin` to `SuperAdmin` OR `Admin`. Two new requirements cover the asymmetry and the JWT contract. Alternatives considered: REMOVED + ADDED (loses the audit log scenario, which is still valid); ADDED only with a separate "Admin can send" requirement (creates two requirements where one with the broader role rule is clearer).

3. **Document the asymmetry in a separate requirement, not as a scenario in the test-email requirement** → The asymmetry is a cross-cutting property of the `AdminController`, not a property of the test-email capability alone. Documenting it as a separate requirement makes it explicit and grep-able. Alternatives considered: a NOTE inside the test-email requirement (gets lost in scenario noise); a separate capability `admin-role-rules` (overkill for one paragraph).

4. **Document the JWT contract in a separate requirement, not as a design note** → The contract (GetByEmailAsync must deep-load navigations) is enforced by a regression test and would otherwise live only in a code comment. Promoting it to a requirement makes it part of the system's documented behavior and ties it to a runnable test. Alternatives considered: a comment in `UserRepository.GetByEmailAsync` (gets lost); an architecture-test in `Architecture.Tests` (no such project yet, would be a bigger change).

## Risks / Trade-offs

- **Spec drift if not enforced** → If the implementation changes again and a developer forgets to update the spec, the spec will silently fall out of sync. Mitigation: the regression tests (`AdminControllerTests.SendTestEmail_AuthorizeAttribute_AcceptsAdminAndSuperAdmin` and `UserConfirmationTokenPersistenceTests.GetByEmailAsync_ReturnsUserRolesLoaded`) fail loudly if the implementation changes; the spec is the source of truth for what the tests must guard.

- **Regression test passes against InMemory but fails against PostgreSQL** → The `GetByEmailAsync` regression test uses the InMemory provider, which doesn't validate SQL. The Include chain is provider-agnostic (it builds an `IQueryable` that EF translates to `LEFT JOIN` in SQL), so this is low-risk, but not zero. Mitigation: the change adds the InMemory regression test as the first guard; a PostgreSQL integration test could be added later via Testcontainers if desired.

- **Action-level `[Authorize]` overrides are easy to accidentally revert** → A future developer who edits `AdminController.cs` to "tidy up" the attributes could remove the action-level override and the class-level rule would re-take effect, blocking Admins. Mitigation: the reflection-based regression test asserts the attribute is present and contains exactly the right role list, catching 100% of reverts.

## Migration Plan

None. This is a documentation-only change. The implementation is already in place and validated by the existing regression tests.

If a future reader finds the spec/code out of sync, the workflow is:
1. Open a new OpenSpec change describing the discrepancy.
2. Add a regression test that fails on the current state.
3. Fix the implementation.
4. Confirm the test passes.
5. Archive the change to sync the spec.

## Open Questions

- Should the test-email rate-limiting non-goal be re-examined? With the `Admin` role now also able to call the endpoint, the volume may grow. Current answer: no, Admin volume is still low and the endpoint is intentionally uncapped for ops.
- Should the asymmetry be lifted entirely (all `AdminController` actions accept `Admin`)? Current answer: no, the dashboard and audit log expose data that requires SuperAdmin trust. Documented as out of scope.
