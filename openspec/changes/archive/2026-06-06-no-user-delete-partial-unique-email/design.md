## Context

The user-management platform exposes CRUD operations on `User`. The
`DELETE` verb is the odd one out: there is no business need to remove
a user permanently (audit log integrity, GDPR-via-anonymization is a
separate track), and the previous soft-delete behaviour was leaking
through a production bug. A user who was soft-deleted (via the
`DELETE /api/users/{id}` action that called `IRepository<T>.DeleteAsync`)
was hidden from the global query filter, so a fresh user with the same
email could not be inserted — the full unique index
`IX_Users_Email` did not consider `DeletedAt`.

## Goals / Non-Goals

**Goals:**
- Public HTTP surface no longer offers any way to delete (hard or
  soft) a user. The only status change is activate/deactivate via
  `PATCH /api/users/{id}/activate`.
- Database permits reuse of an email that was previously held by a
  soft-deleted user. The partial unique index enforces uniqueness
  only among `DeletedAt IS NULL` rows.
- Internal `User.SoftDelete()` and `IRepository<T>.DeleteAsync` are
  kept (for future GDPR anonymization or maintenance) but no public
  route invokes them.

**Non-Goals:**
- No hard-delete (truncate, `DELETE FROM Users`). Out of scope; would
  need separate legal/audit review.
- No anonymization. The product rule is "permanent user identity";
  GDPR is a future track.
- No change to the role, permission, or refresh-token tables.

## Decisions

1. **Remove the action vs return 410 Gone** → Remove. Returning
   `410 Gone` keeps the route discoverable in the codebase and risks
   accidental re-enablement. A clean removal is the strongest signal
   that the product does not support delete.
2. **Partial unique index at the DB layer vs ignore-query-filters in
   the pre-check** → DB layer. The application layer's pre-check
   `GetByEmailAsync` is shared with login, forgot-password and other
   flows; switching to `IgnoreQueryFilters()` would change the
   semantics of those flows. The DB-layer fix is local to the unique
   constraint and does not affect any other flow.
3. **Drop + recreate the index vs `ALTER INDEX ... WHERE ...`** →
   Drop + recreate. PostgreSQL does not support altering an index's
   `WHERE` clause in place, and `DROP INDEX` is non-blocking on read.
4. **Keep the repository `DeleteAsync` method** → Yes. It is the
   only way to set `DeletedAt` on a user, and future GDPR work will
   need it. The constraint is at the HTTP layer, not the repository
   layer.

## Risks / Trade-offs

- **InMemory provider does not enforce unique constraints.** The 4
  new tests added in `test: document soft-delete email-reuse gap`
  assert application-layer behavior (controller calls `AddAsync`,
  repository accepts it). The actual fix is the PostgreSQL migration;
  it must be exercised against a real PostgreSQL instance. The
  migration script is verified with
  `dotnet ef migrations script --idempotent` and the resulting SQL
  is reviewed.
- **Existing rows with duplicate `Email` and `DeletedAt IS NOT NULL`.**
  When the unique index is dropped, the recreation is unconstrained
  for soft-deleted rows, so the migration is safe even if the
  pre-existing data has multiple soft-deleted rows for the same
  email (which would have been impossible under the old index).
- **Role/permission `DELETE` permission becomes dead code.** The
  permission is preserved (not removed) so that audit-log entries
  from the old code path still resolve, but no controller checks
  for it.
