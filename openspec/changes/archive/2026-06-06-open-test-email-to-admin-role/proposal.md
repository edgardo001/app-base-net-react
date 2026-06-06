## Why

The test-email endpoint was originally restricted to `SuperAdmin` only. In practice, the `Admin` role (a tier below SuperAdmin) also needs to verify SMTP configuration when managing user-facing email flows — waiting on a SuperAdmin to test email delivery is a blocker for routine operational work. The implementation was updated to accept `Admin` OR `SuperAdmin` for the test-email endpoint, but the spec still documents the old rule, and the role asymmetry (test-email is more permissive than the other admin endpoints) was never formally captured.

This change formalizes the new role rule and documents the asymmetry as a first-class contract, so that future changes to either the test-email authorization or the broader `AdminController` rules go through a proposal+archive cycle instead of a silent code change.

## What Changes

- **Modify** the `admin-test-email` capability spec: the role rule for the test-email endpoint changes from `SuperAdmin` only to `SuperAdmin` OR `Admin`.
- **Add** a new requirement to the spec that captures the role asymmetry: the test-email endpoint is the only `AdminController` action that accepts the `Admin` role; the dashboard, audit log, and token revocation endpoints remain `SuperAdmin` only.
- **Add** a new requirement that captures the JWT contract: the role/permission claims used to authorize this endpoint come from the `GetByEmailAsync` load (the login flow MUST include `UserRoles` → `Role` → `RolePermissions` → `Permission` for the JWT to carry them). This was the root cause of the production bug that this change also documents.

The backend implementation (`AdminController.SendTestEmail` action-level `[Authorize(Roles = "SuperAdmin,Admin")]`) and the regression test (`AdminControllerTests.SendTestEmail_AuthorizeAttribute_AcceptsAdminAndSuperAdmin`) are already in place in commit `cca0088`. The `GetByEmailAsync` fix and its regression test are already in place in commit `34f768b`. This change is a **documentation/contract sync**, not a code change.

## Capabilities

### New Capabilities
<!-- No new capabilities; this change documents an existing capability. -->

### Modified Capabilities
- `admin-test-email`: the role rule changes from `SuperAdmin` only to `SuperAdmin` OR `Admin`; a new requirement documents the role asymmetry across `AdminController`; a new requirement documents the JWT contract dependency on `GetByEmailAsync`.

## Impact

- **Spec**: `openspec/specs/admin-test-email/spec.md` — `## MODIFIED Requirements` delta (1 modified requirement + 2 new requirements).
- **No code change**: the implementation is already correct (`cca0088` + `34f768b`).
- **No migration**: this is a documentation change only.
- **No API breaking change**: the endpoint contract (`POST /api/admin/test-email` with `{ to: string }` body, 200/400/403/500 responses) is unchanged.
