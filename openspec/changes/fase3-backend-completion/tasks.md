## 1. Soft-Delete User Endpoint

- [ ] 1.1 Add `DELETE /api/users/{id}` endpoint in `UsersController.cs` — find user by ID, check not null (404), check not system user (403), check not self-delete (400), call `SoftDelete()`, save changes, log audit, return 200
- [ ] 1.2 Add unit test `UsersControllerTests.cs` — test 4 scenarios: success (200), not found (404), system user (403), self-delete (400)
- [ ] 1.3 Run `dotnet test app-base-net-react.slnx` — confirm all tests pass

## 2. Avatar Storage Infrastructure

- [ ] 2.1 Create `Application/Common/Interfaces/IFileStorageService.cs` — interface with `SaveFileAsync(Stream, string extension, CancellationToken)`, `GetFilePathAsync(string fileName)`, `DeleteFileAsync(string fileName)`
- [ ] 2.2 Create `Application/Common/Models/StorageOptions.cs` — config class with `BasePath`, `MaxFileSizeBytes`, `AllowedExtensions`
- [ ] 2.3 Create `Infrastructure/Storage/LocalFileStorageService.cs` — implement `IFileStorageService`, use `Path.GetRandomFileName()` for filenames, validate extension and size, create directory if not exists
- [ ] 2.4 Register `IFileStorageService` + `StorageOptions` in `Infrastructure/DependencyInjection.cs`
- [ ] 2.5 Add `Storage` section to `appsettings.json` with defaults (`BasePath`, `MaxFileSizeBytes: 5242880`, `AllowedExtensions: [".jpg",".jpeg",".png",".webp"]`)

## 3. Avatar Upload Endpoints

- [ ] 3.1 Add `POST /api/users/{id}/avatar` endpoint in `UsersController.cs` — validate file (extension, size), call `IFileStorageService.SaveFileAsync`, update `User.SetAvatar(fileName)`, return 200 with filename
- [ ] 3.2 Add `PUT /api/profile/avatar` endpoint in `ProfileController.cs` — same validation, update authenticated user's avatar
- [ ] 3.3 Add `GET /api/users/{id}/avatar` endpoint in `UsersController.cs` — find user, get file path, serve file with appropriate `Content-Type`, return 404 if no avatar
- [ ] 3.4 Add unit tests for avatar endpoints — test upload success, invalid file type, file too large, avatar not found
- [ ] 3.5 Run `dotnet test app-base-net-react.slnx` — confirm all tests pass

## 4. Users by Role Endpoint

- [ ] 4.1 Add `GET /api/roles/{id}/users` endpoint in `RolesController.cs` — call `IUserRepository.GetUsersByRoleAsync`, map to DTO, return 200 or 404
- [ ] 4.2 Add unit test `RolesControllerTests.cs` — test: success (200), role not found (404), role with no users (200 empty array)
- [ ] 4.3 Run `dotnet test app-base-net-react.slnx` — confirm all tests pass

## 5. Final Validation

- [ ] 5.1 `dotnet test app-base-net-react.slnx` — all tests green
- [ ] 5.2 `dotnet build app-base-net-react.slnx` — no errors
- [ ] 5.3 Verify no DB migration needed (no schema changes)
