# Spec Delta: user-creation

## ADDED Requirements

### Requirement: User identity is permanent (no public delete)

The public HTTP surface MUST NOT offer any operation that removes a
user from the system. A user is created once, may be activated and
deactivated, may have its name and roles updated, and may receive
resend-onboarding emails. The only status-change endpoint is
`PATCH /api/users/{id}/activate`, which toggles `IsActive`; the row
itself remains in `Users` indefinitely.

The system MUST NOT expose a `DELETE /api/users/{id}` route. The
frontend MUST NOT offer a delete affordance for a user. The
repository's `DeleteAsync` and the entity's `SoftDelete` methods are
internal-only and are not reachable from any controller action.

#### Scenario: API does not expose a user delete route
- **WHEN** the public OpenAPI/Scalar documentation is consulted
- **THEN** there is no `DELETE /api/users/{id}` (or equivalent) entry
- **AND** the `UsersController` does not declare any `[HttpDelete]`
  action that targets a user

#### Scenario: Frontend does not show a delete button
- **WHEN** an administrator opens the `/users` page
- **THEN** each user row has only edit, activate/deactivate, and
  resend-onboarding actions
- **AND** there is no trash or delete button

### Requirement: Email and NormalizedEmail are partially unique among active users

The system MUST define the unique indexes `IX_Users_Email` and
`IX_Users_NormalizedEmail` as partial unique indexes with filter
`WHERE "DeletedAt" IS NULL` (PostgreSQL). Two active users MUST NOT
share the same `Email` or `NormalizedEmail`; any number of
soft-deleted users MAY share an email with an active user.

The application's `GetByEmailAsync` lookup continues to use the
default query filter (`DeletedAt == null`), so a soft-deleted user
is invisible to the lookup and a fresh user with the same email
may be created.

#### Scenario: Two active users cannot share an email
- **GIVEN** an active user with email `a@x.com`
- **WHEN** the API receives `POST /api/users` with email `a@x.com`
- **THEN** PostgreSQL rejects the insert with error `23505`
  `IX_Users_Email`
- **AND** the API returns `409 Conflict`

#### Scenario: A re-used email after deactivation is accepted
- **GIVEN** a user with email `a@x.com` exists and has been
  deactivated (or soft-deleted)
- **WHEN** the API receives `POST /api/users` with email `a@x.com`
- **THEN** the `GetByEmailAsync` pre-check returns `null` because
  the existing user is hidden by the global query filter
- **AND** PostgreSQL accepts the insert because the unique index is
  partial (`WHERE "DeletedAt" IS NULL`)
- **AND** the API returns `200 OK`
