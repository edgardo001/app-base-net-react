# users-management Specification

## Purpose
TBD - created by archiving change cqrs-users-management. Update Purpose after archive.
## Requirements
### Requirement: GetUsers Returns Paged User List
The system SHALL return a paginated list of users when an authenticated user calls `GET /api/users` with valid query parameters (page, pageSize, search, sortBy, sortDesc). The response SHALL contain a `PagedResponse<UserDto>` with items, total count, page number, page size, and total pages.

#### Scenario: Valid request returns paged results
- **WHEN** an authenticated user calls `GET /api/users?page=1&pageSize=10`
- **THEN** the system MUST return HTTP 200 with `ApiResponse<PagedResponse<UserDto>>` containing up to 10 users, total count, and pagination metadata

#### Scenario: Search filters by email or name
- **WHEN** an authenticated user calls `GET /api/users?search=john`
- **THEN** the system MUST return only users whose email, first name, or last name contains "john" (case-insensitive)

#### Scenario: Sorting orders results correctly
- **WHEN** an authenticated user calls `GET /api/users?sortBy=email&sortDesc=true`
- **THEN** the system MUST return users ordered by email in descending order

### Requirement: GetUser Returns User Detail With Roles
The system SHALL return a single user's details including assigned roles when an authenticated user calls `GET /api/users/{id}`.

#### Scenario: Existing user returns detail
- **WHEN** an authenticated user calls `GET /api/users/{id}` for an existing user
- **THEN** the system MUST return HTTP 200 with `ApiResponse<UserDetailDto>` containing user fields and a `roles` array

#### Scenario: Non-existent user returns 404
- **WHEN** an authenticated user calls `GET /api/users/{id}` for a non-existent user
- **THEN** the system MUST return HTTP 404 with `ApiResponse<object>.Fail("User not found")`

### Requirement: CreateUser Handler Processes User Creation
The `CreateUserCommandHandler` SHALL process the `CreateUserCommand`, performing: duplicate email check, password generation via `IRandomPasswordGenerator`, password hashing via `IPasswordHasherService`, user entity creation via `User.Create(...)`, role assignment, email confirmation token generation, and persistence. The handler SHALL return a `CreateUserOutcome` with `Success`, `DuplicateEmail`, or `Error` results.

#### Scenario: Valid command creates user
- **WHEN** `CreateUserCommandHandler.Handle` is called with a valid command containing email, firstName, lastName, and optional roleIds
- **THEN** the handler MUST generate a temporary password, hash it, create the user with `EmailConfirmed = false`, call `ForcePasswordChange()`, assign roles, generate confirmation token, persist via `IUnitOfWork`, and return `CreateUserOutcome.Success(id, email)`

#### Scenario: Duplicate email returns outcome error
- **WHEN** `CreateUserCommandHandler.Handle` is called with an email that already exists (non-deleted user)
- **THEN** the handler MUST return `CreateUserOutcome.DuplicateEmail` without creating a user

#### Scenario: Soft-deleted user email is reusable
- **WHEN** `CreateUserCommandHandler.Handle` is called with an email that only exists on a soft-deleted user
- **THEN** the handler MUST proceed with creation (the partial unique index allows this)

### Requirement: UpdateUser Handler Updates User Profile And Roles
The `UpdateUserCommandHandler` SHALL update an existing user's profile fields (firstName, lastName) and reassign roles if provided.

#### Scenario: Valid command updates user
- **WHEN** `UpdateUserCommandHandler.Handle` is called with a valid command for an existing user
- **THEN** the handler MUST call `user.UpdateProfile(firstName, lastName)`, reassign roles if `roleIds` is provided, persist via `IUnitOfWork.SaveChangesAsync`, and return `UpdateUserOutcome.Success`

#### Scenario: Non-existent user returns outcome error
- **WHEN** `UpdateUserCommandHandler.Handle` is called with a non-existent userId
- **THEN** the handler MUST return `UpdateUserOutcome.UserNotFound`

### Requirement: DeleteUser Handler Performs Soft Delete
The `DeleteUserCommandHandler SHALL perform a soft delete on an existing user, preventing self-deletion.

#### Scenario: Valid command soft-deletes user
- **WHEN** `DeleteUserCommandHandler.Handle` is called with a valid command for an existing user (not the current user)
- **THEN** the handler MUST call `user.SoftDelete()`, persist via `IUnitOfWork.SaveChangesAsync`, and return `DeleteUserOutcome.Success`

#### Scenario: Self-deletion returns outcome error
- **WHEN** `DeleteUserCommandHandler.Handle` is called where `command.UserId == command.CurrentUserId`
- **THEN** the handler MUST return `DeleteUserOutcome.CannotDeleteSelf`

#### Scenario: Non-existent user returns outcome error
- **WHEN** `DeleteUserCommandHandler.Handle` is called with a non-existent userId
- **THEN** the handler MUST return `DeleteUserOutcome.UserNotFound`

### Requirement: ToggleActive Handler Activates Or Deactivates User
The `ToggleActiveCommandHandler` SHALL toggle the `IsActive` state of an existing user.

#### Scenario: Valid command toggles active state
- **WHEN** `ToggleActiveCommandHandler.Handle` is called with a valid command for an existing user
- **THEN** the handler MUST call `user.SetActive(!user.IsActive)`, persist via `IUnitOfWork.SaveChangesAsync`, and return `ToggleActiveOutcome.Success(isActive)`

#### Scenario: Non-existent user returns outcome error
- **WHEN** `ToggleActiveCommandHandler.Handle` is called with a non-existent userId
- **THEN** the handler MUST return `ToggleActiveOutcome.UserNotFound`

### Requirement: AdminResetPassword Handler Generates Temporary Password
The `AdminResetPasswordCommandHandler` SHALL generate a temporary password, hash it, confirm the user's email, and send a notification email.

#### Scenario: Valid command resets password
- **WHEN** `AdminResetPasswordCommandHandler.Handle` is called with a valid command for an existing user
- **THEN** the handler MUST generate a temporary password via `IRandomPasswordGenerator`, hash it via `IPasswordHasherService`, call `user.ConfirmEmail()`, persist via `IUnitOfWork.SaveChangesAsync`, publish a `PasswordResetByAdminNotification`, and return `AdminResetPasswordOutcome.Success(tempPassword)`

#### Scenario: Non-existent user returns outcome error
- **WHEN** `AdminResetPasswordCommandHandler.Handle` is called with a non-existent userId
- **THEN** the handler MUST return `AdminResetPasswordOutcome.UserNotFound`

### Requirement: RevokeUserTokens Handler Revokes All Refresh Tokens
The `RevokeUserTokensCommandHandler` SHALL revoke all refresh tokens for a specific user.

#### Scenario: Valid command revokes tokens
- **WHEN** `RevokeUserTokensCommandHandler.Handle` is called with a valid command for an existing user
- **THEN** the handler MUST call `IRefreshTokenRepository.RevokeAllForUserAsync(userId)`, persist via `IUnitOfWork.SaveChangesAsync`, publish a `TokensRevokedNotification`, and return `RevokeUserTokensOutcome.Success`

#### Scenario: Non-existent user returns outcome error
- **WHEN** `RevokeUserTokensCommandHandler.Handle` is called with a non-existent userId
- **THEN** the handler MUST return `RevokeUserTokensOutcome.UserNotFound`

### Requirement: UploadAvatar Handler Saves User Avatar
The `UploadAvatarCommandHandler` SHALL validate the uploaded file (extension, size), save it via `IFileStorageService`, and update the user's `AvatarPath`.

#### Scenario: Valid file uploads successfully
- **WHEN** `UploadAvatarCommandHandler.Handle` is called with a valid file (allowed extension, within size limit) for an existing user
- **THEN** the handler MUST validate the file, save it via `IFileStorageService.SaveFileAsync`, set `user.AvatarPath`, persist via `IUnitOfWork.SaveChangesAsync`, and return `UploadAvatarOutcome.Success(filePath)`

#### Scenario: Invalid extension returns outcome error
- **WHEN** `UploadAvatarCommandHandler.Handle` is called with a file extension not in the allowed list
- **THEN** the handler MUST return `UploadAvatarOutcome.InvalidExtension` without saving

#### Scenario: File too large returns outcome error
- **WHEN** `UploadAvatarCommandHandler.Handle` is called with a file exceeding `StorageOptions.MaxFileSize`
- **THEN** the handler MUST return `UploadAvatarOutcome.FileTooLarge` without saving

#### Scenario: Non-existent user returns outcome error
- **WHEN** `UploadAvatarCommandHandler.Handle` is called with a non-existent userId
- **THEN** the handler MUST return `UploadAvatarOutcome.UserNotFound`

### Requirement: GetAvatar Handler Returns Avatar File Path
The `GetAvatarQueryHandler` SHALL retrieve the avatar file path and content type for an existing user.

#### Scenario: User with avatar returns path
- **WHEN** `GetAvatarQueryHandler.Handle` is called for a user with an `AvatarPath` set
- **THEN** the handler MUST return `GetAvatarOutcome.Success(filePath, contentType)`

#### Scenario: User without avatar returns not found
- **WHEN** `GetAvatarQueryHandler.Handle` is called for a user with no `AvatarPath`
- **THEN** the handler MUST return `GetAvatarOutcome.NoAvatar`

### Requirement: UsersNotifications Defines Audit Notifications
The system SHALL define notification records in `Application/Features/Users/Notifications/UsersNotifications.cs` for all user management operations that require audit logging.

#### Scenario: Notifications are defined
- **WHEN** the application starts
- **THEN** the following notification records MUST be available: `UserCreatedNotification`, `UserUpdatedNotification`, `UserDeletedNotification`, `UserActivatedNotification`, `UserDeactivatedNotification`, `PasswordResetByAdminNotification`, `TokensRevokedNotification`, `AvatarUpdatedNotification`

### Requirement: Audit Notification Handlers Log Operations
The system SHALL implement `INotificationHandler<T>` for each Users notification, calling `IAuditService.LogAsync` with appropriate action, entity type, entity ID, and audit metadata.

#### Scenario: UserCreated notification triggers audit log
- **WHEN** a `UserCreatedNotification` is published
- **THEN** the handler MUST call `IAuditService.LogAsync` with action `"UserCreated"`, entityType `"User"`, entityId the new user's ID, and the IP/user-agent from the notification

#### Scenario: All notification handlers are registered
- **WHEN** the application starts
- **THEN** all Users notification handlers MUST be registered in the DI container via `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`

