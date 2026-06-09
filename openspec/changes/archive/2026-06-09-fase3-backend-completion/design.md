## Context

The User Management Platform backend is ~85% feature-complete for Fase 3 (Gestión de Usuarios). Three backend capabilities remain:

1. **Soft-delete endpoint**: `BaseEntity.SoftDelete()` and `GenericRepository.DeleteAsync()` already implement soft-delete logic. Global query filters exclude `DeletedAt IS NULL`. But no HTTP endpoint exposes this for users.
2. **Avatar storage**: `User.SetAvatar(path)` exists in the domain. No storage infrastructure, no endpoints.
3. **Users by role**: `IUserRepository.GetUsersByRoleAsync(Guid)` is implemented. No HTTP endpoint.

Current architecture: Controllers orchestrate via `IUnitOfWork` + services. CQRS migration is in progress (Auth features migrated, Users features pending).

## Goals / Non-Goals

**Goals:**
- Expose `DELETE /api/users/{id}` with soft-delete, system-user guard, and audit logging
- Implement `IFileStorageService` for local file storage with avatar-specific endpoints
- Expose `GET /api/roles/{id}/users` endpoint
- All new endpoints must have unit tests (Regla de Oro)

**Non-Goals:**
- CQRS migration for Users CRUD (separate change)
- S3/cloud storage (future, `IFileStorageService` makes it swappable)
- Avatar webcam capture (frontend concern, separate task)
- Image compression/resizing on backend (frontend handles before upload)

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Soft-delete response** | `200 OK` with `ApiResponse<object>` | Consistent with existing DELETE patterns. `404` if user not found. `403` if system user. |
| **File storage abstraction** | `IFileStorageService` interface in Application layer | Follows hexagonal architecture. Infrastructure implements `LocalFileStorageService`. S3 adapter possible later. |
| **File naming** | `Path.GetRandomFileName()` + original extension | Prevents path traversal, collision, and information leakage. Never store original filename. |
| **File validation** | Extension + MIME type + size (5 MB) | Defense in depth. Extension check is fast first filter; MIME type via `Content-Type` header; size from `Content-Length`. |
| **Storage path** | Configurable via `Storage:BasePath` in appsettings | Default `/app/storage/avatars` in Docker. Local dev uses `storage/avatars` relative to project. |
| **Users-by-role endpoint** | `GET /api/roles/{id}/users` returns `IReadOnlyList<UserDto>` | Reuses `GetUsersByRoleAsync`. Returns lightweight DTO (no sensitive fields). |

```
┌──────────────────────────────────────────────────────────────┐
│                    ARCHITECTURE DIAGRAM                       │
└──────────────────────────────────────────────────────────────┘

UsersController                    RolesController
  │ DELETE /{id}                     │ GET /{id}/users
  │ POST /{id}/avatar               │
  │                                  │
  ▼                                  ▼
IUnitOfWork                      IUnitOfWork.Users.GetByRoleAsync()
  .Users.GetByIdAsync()              │
  .Users.DeleteAsync()               │
  .SaveChangesAsync()                │
  │                                  │
  ▼                                  │
Domain Guards                        │
  User.SoftDelete()                  │
  (checks IsSystem)                  │
  │                                  │
  ▼                                  │
ProfileController                    │
  │ PUT /avatar                      │
  ▼                                  │
IFileStorageService                  │
  .SaveFileAsync(stream, ext)        │
  .GetFilePathAsync(filename)        │
  .DeleteFileAsync(filename)         │
  │                                  │
  ▼                                  │
Infrastructure/Storage               │
  LocalFileStorageService            │
  (writes to Storage.BasePath)       │
```

## Risks / Trade-offs

- **[Security] Malicious file upload** → Validate extension allowlist (`[".jpg",".jpeg",".png",".webp"]`), MIME type, max 5 MB. Use random filenames. Never serve from user-controlled path.
- **[Docker] Writable volume needed** → `Storage.BasePath` must be a mounted volume in docker-compose. Add `avatars:/app/storage/avatars` volume.
- **[Soft-delete cascade]** → Global query filters handle RefreshToken/UserRole exclusion. No cascade needed because `DeletedAt` filter excludes the user from all queries.
- **[Frontend dependency]** → Avatar endpoints are useless without frontend upload UI. But backend can ship independently — no breaking changes.
