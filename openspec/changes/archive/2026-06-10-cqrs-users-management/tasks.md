# Implementation Tasks: cqrs-users-management

## 1. Baseline — Regla de Oro

> **Ningún cambio debe aplicarse sin antes verificar explícitamente que la funcionalidad original tiene un test unitario que la cubra. Si no lo tiene, se debe crear el test, validar que funcione (`dotnet test`), y luego aplicar el cambio.**

- [x] 1.1 Verify all existing `UsersControllerTests` pass: `dotnet test src/backend/AppBaseNetReact.WebApi.Tests --filter "UsersController"`
- [x] 1.2 Document any missing test coverage identified during analysis (create follow-up tasks if needed)

## 2. Notifications Infrastructure

- [x] 2.1 Create `Application/Features/Users/Notifications/UsersNotifications.cs` with all 8 notification records
- [x] 2.2 Create notification handlers in `Infrastructure/Notifications/` for audit logging (8 handlers)
- [x] 2.3 Create `Application.Tests/Features/Users/Notifications/UsersAuditHandlerTests.cs` with tests for each handler
- [x] 2.4 Run `dotnet test` to verify all notification handler tests pass
- [x] 2.5 Enrich `UserCreatedNotification` with email template data (FirstName, ConfirmationToken, TemporaryPassword, FrontendBaseUrl)
- [x] 2.6 Enrich `PasswordResetByAdminNotification` with email template data (TemporaryPassword, LoginLink)
- [x] 2.7 Create `UserCreatedEmailHandler` and `PasswordResetByAdminEmailHandler`
- [x] 2.8 Create `UsersEmailHandlerTests` with tests for email handlers
- [x] 2.9 Run `dotnet test` to verify all notification tests pass

## 3. Queries — GetUsers and GetUser

- [x] 3.1 Create `Features/Users/Queries/GetUsers/GetUsersQuery.cs` with query parameters (page, pageSize, search, sortBy, sortDesc)
- [x] 3.2 Create `Features/Users/Queries/GetUsers/GetUsersQueryHandler.cs` with pagination logic
- [x] 3.3 Create `Features/Users/Queries/GetUsers/GetUsersQueryValidator.cs` with FluentValidation rules
- [x] 3.4 Create `Features/Users/Queries/GetUsers/GetUsersResponse.cs` with `UserDto` and `PagedResponse<T>`
- [x] 3.5 Create `Application.Tests/Features/Users/Queries/GetUsers/GetUsersQueryHandlerTests.cs`
- [x] 3.6 Create `Features/Users/Queries/GetUser/GetUserQuery.cs`
- [x] 3.7 Create `Features/Users/Queries/GetUser/GetUserQueryHandler.cs`
- [x] 3.8 Create `Features/Users/Queries/GetUser/GetUserQueryValidator.cs`
- [x] 3.9 Create `Features/Users/Queries/GetUser/GetUserResponse.cs` with `UserDetailDto` and `RoleDto`
- [x] 3.10 Create `Application.Tests/Features/Users/Queries/GetUser/GetUserQueryHandlerTests.cs`
- [x] 3.11 Update `UsersController` to use `IMediator.Send` for GetUsers and GetUser
- [x] 3.12 Update `UsersControllerTests` to mock `IMediator` instead of `IUnitOfWork` for queries
- [x] 3.13 Run `dotnet test` to verify all tests pass

## 4. Simple Commands — ToggleActive and RevokeTokens

- [x] 4.1 Create `Features/Users/Commands/ToggleActive/ToggleActiveCommand.cs`
- [x] 4.2 Create `Features/Users/Commands/ToggleActive/ToggleActiveCommandHandler.cs`
- [x] 4.3 Create `Features/Users/Commands/ToggleActive/ToggleActiveCommandValidator.cs`
- [x] 4.4 Create `Features/Users/Commands/ToggleActive/ToggleActiveOutcome.cs`
- [x] 4.5 Create `Application.Tests/Features/Users/Commands/ToggleActive/ToggleActiveCommandHandlerTests.cs`
- [x] 4.6 Create `Features/Users/Commands/RevokeTokens/RevokeTokensCommand.cs`
- [x] 4.7 Create `Features/Users/Commands/RevokeTokens/RevokeTokensCommandHandler.cs`
- [x] 4.8 Create `Features/Users/Commands/RevokeTokens/RevokeTokensCommandValidator.cs`
- [x] 4.9 Create `Features/Users/Commands/RevokeTokens/RevokeTokensOutcome.cs`
- [x] 4.10 Create `Application.Tests/Features/Users/Commands/RevokeTokens/RevokeTokensCommandHandlerTests.cs`
- [x] 4.11 Update `UsersController` to use `IMediator.Send` for ToggleActive and RevokeTokens
- [x] 4.12 Update `UsersControllerTests` to mock `IMediator` for these commands
- [x] 4.13 Run `dotnet test` to verify all tests pass

## 5. Moderate Commands — UpdateUser and DeleteUser

- [x] 5.1 Create `Features/Users/Commands/UpdateUser/UpdateUserCommand.cs`
- [x] 5.2 Create `Features/Users/Commands/UpdateUser/UpdateUserCommandHandler.cs`
- [x] 5.3 Create `Features/Users/Commands/UpdateUser/UpdateUserCommandValidator.cs`
- [x] 5.4 Create `Features/Users/Commands/UpdateUser/UpdateUserOutcome.cs`
- [x] 5.5 Create `Application.Tests/Features/Users/Commands/UpdateUser/UpdateUserCommandHandlerTests.cs`
- [x] 5.6 Create `Features/Users/Commands/DeleteUser/DeleteUserCommand.cs`
- [x] 5.7 Create `Features/Users/Commands/DeleteUser/DeleteUserCommandHandler.cs`
- [x] 5.8 Create `Features/Users/Commands/DeleteUser/DeleteUserCommandValidator.cs`
- [x] 5.9 Create `Features/Users/Commands/DeleteUser/DeleteUserOutcome.cs`
- [x] 5.10 Create `Application.Tests/Features/Users/Commands/DeleteUser/DeleteUserCommandHandlerTests.cs`
- [x] 5.11 Update `UsersController` to use `IMediator.Send` for UpdateUser and DeleteUser
- [x] 5.12 Update `UsersControllerTests` to mock `IMediator` for these commands
- [x] 5.13 Run `dotnet test` to verify all tests pass

## 6. Email-Dependent Command — AdminResetPassword

- [x] 6.1 Create `Features/Users/Commands/AdminResetPassword/AdminResetPasswordCommand.cs`
- [x] 6.2 Create `Features/Users/Commands/AdminResetPassword/AdminResetPasswordCommandHandler.cs`
- [x] 6.3 Create `Features/Users/Commands/AdminResetPassword/AdminResetPasswordCommandValidator.cs`
- [x] 6.4 Create `Features/Users/Commands/AdminResetPassword/AdminResetPasswordOutcome.cs`
- [x] 6.5 Create `Application.Tests/Features/Users/Commands/AdminResetPassword/AdminResetPasswordCommandHandlerTests.cs`
- [x] 6.6 Update `UsersController` to use `IMediator.Send` for ResetPassword
- [x] 6.7 Update `UsersControllerTests` to mock `IMediator` for ResetPassword
- [x] 6.8 Run `dotnet test` to verify all tests pass

## 7. Complex Command — CreateUser

- [x] 7.1 Create `Features/Users/Commands/CreateUser/CreateUserCommand.cs`
- [x] 7.2 Create `Features/Users/Commands/CreateUser/CreateUserCommandHandler.cs`
- [x] 7.3 Create `Features/Users/Commands/CreateUser/CreateUserCommandValidator.cs`
- [x] 7.4 Create `Features/Users/Commands/CreateUser/CreateUserOutcome.cs`
- [x] 7.5 Create `Features/Users/Commands/CreateUser/CreateUserResponse.cs`
- [x] 7.6 Create `Application.Tests/Features/Users/Commands/CreateUser/CreateUserCommandHandlerTests.cs`
- [x] 7.7 Update `UsersController` to use `IMediator.Send` for CreateUser
- [x] 7.8 Update `UsersControllerTests` to mock `IMediator` for CreateUser
- [x] 7.9 Run `dotnet test` to verify all tests pass

## 8. File I/O Commands — UploadAvatar and GetAvatar

- [x] 8.1 Create `Features/Users/Commands/UploadAvatar/UploadAvatarCommand.cs`
- [x] 8.2 Create `Features/Users/Commands/UploadAvatar/UploadAvatarCommandHandler.cs`
- [x] 8.3 Create `Features/Users/Commands/UploadAvatar/UploadAvatarCommandValidator.cs`
- [x] 8.4 Create `Features/Users/Commands/UploadAvatar/UploadAvatarOutcome.cs`
- [x] 8.5 Create `Application.Tests/Features/Users/Commands/UploadAvatar/UploadAvatarCommandHandlerTests.cs`
- [x] 8.6 Create `Features/Users/Queries/GetAvatar/GetAvatarQuery.cs`
- [x] 8.7 Create `Features/Users/Queries/GetAvatar/GetAvatarQueryHandler.cs`
- [x] 8.8 Create `Features/Users/Queries/GetAvatar/GetAvatarQueryValidator.cs`
- [x] 8.9 Create `Features/Users/Queries/GetAvatar/GetAvatarOutcome.cs`
- [x] 8.10 Create `Application.Tests/Features/Users/Queries/GetAvatar/GetAvatarQueryHandlerTests.cs`
- [x] 8.11 Update `UsersController` to use `IMediator.Send` for UploadAvatar and GetAvatar
- [x] 8.12 Update `UsersControllerTests` to mock `IMediator` for avatar operations
- [x] 8.13 Run `dotnet test` to verify all tests pass

## 9. Final Verification

- [x] 9.1 Run full test suite: `dotnet test app-base-net-react.slnx` — 325 tests, 0 failures
- [x] 9.2 Verify all 23 controller tests pass (API contract preservation)
- [x] 9.3 Verify all new handler tests pass
- [x] 9.4 Verify all notification handler tests pass
- [x] 9.5 Verify `UsersController` is thin (only `IMediator` + `IConfiguration` injection, no `IUnitOfWork`)
- [x] 9.6 Build solution: `dotnet build app-base-net-react.slnx` — 0 errors
