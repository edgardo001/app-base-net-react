## Why

The Users module is the last major domain with inline business logic in controllers. Currently, `UsersController` orchestrates 12 actions directly (injecting `IUnitOfWork`, calling domain methods, sending emails, writing audit logs), while the Auth module is fully migrated to CQRS with MediatR handlers. This inconsistency makes the codebase harder to maintain, test, and extend. Completing the CQRS migration for Users will establish a consistent architectural pattern across all domains, improve testability (handler-level unit tests), and prepare the foundation for future features (e.g., user import/export, bulk operations).

## What Changes

- **Migrate 11 controller actions to MediatR command/query handlers**:
  - 2 Queries: `GetUsers`, `GetUser`
  - 7 Commands: `CreateUser`, `UpdateUser`, `DeleteUser`, `ToggleActive`, `AdminResetPassword`, `RevokeUserTokens`, `UploadUserAvatar`
  - 1 Query: `GetUserAvatar` (file serving)
  - 1 already migrated: `ResendOnboardingEmail` (no change)

- **Create notification handlers for audit logging** (8 notifications):
  - `UserCreatedNotification`, `UserUpdatedNotification`, `UserDeletedNotification`
  - `UserActivatedNotification`, `UserDeactivatedNotification`
  - `PasswordResetByAdminNotification`, `TokensRevokedNotification`, `AvatarUpdatedNotification`

- **Create FluentValidation validators** for each command/query

- **Create Outcome types** for each command (matching Auth pattern)

- **Move DTOs** from inline in `UsersController.cs` to feature-scoped response types

- **Thin controllers**: Controllers become HTTP adapters (extract context → Send → map response)

- **Preserve API contracts**: Same HTTP status codes, response shapes, error messages

## Capabilities

### New Capabilities

- `users-management`: CQRS handlers for user CRUD, pagination, role assignment, avatar upload, admin password reset, and token revocation

### Modified Capabilities

- `user-creation`: Extend with formal CQRS handler (currently inline in controller). Requirements remain the same, implementation moves to handler.

## Impact

- **Backend**: `Application/Features/Users/` (new handlers, validators, outcomes, notifications), `WebApi/Controllers/UsersController.cs` (slim down), `Infrastructure/Notifications/` (audit handlers)
- **Tests**: `Application.Tests/Features/Users/` (new handler tests), `WebApi.Tests/Controllers/UsersControllerTests.cs` (update to mock IMediator)
- **API**: No breaking changes — same endpoints, same response shapes
- **Dependencies**: No new NuGet packages (MediatR already configured)
