## Why

The product rule is that user identity is permanent: the only legitimate
operations on a user are create, edit, activate/deactivate, and resend
onboarding. There is no legitimate use case for hard-deleting a user, and
no use case for soft-deleting via the API either. The previous
`DELETE /api/users/{id}` action (which used the repository's soft-delete
mechanism) was conceptually wrong: a user that can be soft-deleted can
also be re-created with the same email, but the full unique index on
`IX_Users_Email` did not allow it, surfacing as a 23505
duplicate-key error in production.

## What Changes

- **Remove** the `DELETE /api/users/{id}` action from `UsersController`.
- **Remove** the trash button, `deleteUser` function and `Trash2` import
  from `pages/users.tsx`.
- **Convert** the unique indexes `IX_Users_Email` and
  `IX_Users_NormalizedEmail` to **partial unique indexes** constrained to
  `WHERE "DeletedAt" IS NULL` (PostgreSQL). A new EF Core migration drops
  the existing full unique indexes and recreates them with the filter.
- **Preserve** the `User.SoftDelete()` method and `IRepository<T>.DeleteAsync`
  for internal/maintenance use; the soft-delete is reachable only from
  infrastructure code, never from the public HTTP surface.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `user-creation`: enforces the no-delete + partial-unique-email rule.

## Impact

- **Backend**: `UsersController.cs` (action removed), `EntityConfigurations.cs`
  (filter added), `Migrations/20260606143024_MakeEmailAndNormalizedEmailPartialUniqueIndexes.cs`
  (NEW).
- **Frontend**: `pages/users.tsx` (button + function + import removed).
- **Database**: existing rows are unaffected; the migration only changes
  index definitions, not data.
