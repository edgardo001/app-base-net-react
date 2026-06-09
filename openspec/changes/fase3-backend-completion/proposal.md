## Why

The Fase 3 (Gestión de Usuarios) is ~85% complete. Three backend tasks remain to close the gap: soft-delete endpoint for users, avatar file storage with endpoints, and a "users by role" endpoint. These unlock corresponding frontend work and bring the platform closer to the full RBAC + profile management promised in `planInicial.ia.md`.

## What Changes

- **ADD** `DELETE /api/users/{id}` — soft-delete a user (marks `DeletedAt`, excluded by global query filters). Returns 200 on success, 404 if not found, 403 if trying to delete a system user.
- **ADD** Avatar storage infrastructure — `IFileStorageService` port + `LocalFileStorageService` adapter. Endpoints: `POST /api/users/{id}/avatar`, `GET /api/users/{id}/avatar`, `PUT /api/profile/avatar`. Validates extension, MIME type, and max size (5 MB). Stores files with random names under configurable `Storage.BasePath`.
- **ADD** `GET /api/roles/{id}/users` — returns users assigned to a specific role. Reuses existing `IUserRepository.GetUsersByRoleAsync`.

## Capabilities

### New Capabilities
- `user-soft-delete`: Soft-delete user endpoint with domain guard and tests
- `avatar-storage`: File storage service, avatar upload/download endpoints, configuration
- `users-by-role`: Endpoint to list users assigned to a given role

### Modified Capabilities
<!-- No existing spec-level behavior changes. All three are additive features. -->

## Impact

- **Backend controllers**: `UsersController` (+2 endpoints), `RolesController` (+1 endpoint), `ProfileController` (+1 endpoint)
- **New files**: `IFileStorageService`, `LocalFileStorageService`, `StorageOptions` config class
- **Configuration**: New `Storage` section in `appsettings.json` / `.env`
- **Docker**: Backend container needs writable volume for `Storage.BasePath`
- **Tests**: +6-8 new unit tests across controller and service layers
