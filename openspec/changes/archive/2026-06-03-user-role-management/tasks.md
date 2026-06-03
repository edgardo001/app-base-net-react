## 1. Users Controller

- [x] 1.1 Implement GET /api/users with pagination, search, sort (page, pageSize, search, sortBy, sortDesc)
- [x] 1.2 Implement GET /api/users/{id} returning UserDetailDto with roles
- [x] 1.3 Implement POST /api/users with CreateUserRequest (email, firstName, lastName, password, roleIds)
- [x] 1.4 Implement PUT /api/users/{id} with UpdateUserRequest (firstName, lastName, roleIds)
- [x] 1.5 Implement DELETE /api/users/{id} with soft delete
- [x] 1.6 Implement PATCH /api/users/{id}/activate with ToggleActiveRequest
- [x] 1.7 Implement PATCH /api/users/{id}/reset-password generating temporary password
- [x] 1.8 Implement PATCH /api/users/{id}/revoke-tokens

## 2. Roles Controller

- [x] 2.1 Implement GET /api/roles returning all roles with RoleDetailDto
- [x] 2.2 Implement GET /api/roles/{id} returning role with permission assignments
- [x] 2.3 Implement POST /api/roles creating new role with audit logging
- [x] 2.4 Implement PUT /api/roles/{id} updating role (block if IsSystem)
- [x] 2.5 Implement DELETE /api/roles/{id} deleting role (block if IsSystem)
- [x] 2.6 Implement PATCH /api/roles/{id}/permissions replacing all role permissions

## 3. Permissions Controller

- [x] 3.1 Implement GET /api/permissions returning all permissions
- [x] 3.2 Implement GET /api/permissions/modules returning permissions grouped by module

## 4. Profile Controller

- [x] 4.1 Implement GET /api/profile returning current user info from JWT sub claim
- [x] 4.2 Implement PUT /api/profile updating firstName/lastName with audit logging
- [x] 4.3 Implement GET /api/profile/activity returning last 20 audit entries

## 5. Admin Controller

- [x] 5.1 Implement GET /api/admin/dashboard with user metrics (total, active, inactive, new 7d)
- [x] 5.2 Implement GET /api/admin/audit-log with pagination
- [x] 5.3 Implement POST /api/admin/revoke-all-tokens with global revocation
- [x] 5.4 Apply [Authorize(Roles = "SuperAdmin")] to entire AdminController

## 6. DTOs and Response Models

- [x] 6.1 Create UserDto and UserDetailDto (with roles list)
- [x] 6.2 Create RoleDto and RoleDetailDto
- [x] 6.3 Create PagedResponse<T> for paginated results
- [x] 6.4 Create ApiResponse<T> wrapper with Data, Message, Success properties
- [x] 6.5 Create request records: CreateUserRequest, UpdateUserRequest, ToggleActiveRequest, UpdateProfileRequest, UpdatePermissionsRequest

## 7. User Repository Specialized Queries

- [x] 7.1 Implement IUserRepository.GetByEmailAsync (for login)
- [x] 7.2 Implement IUserRepository.GetPagedAsync with search, sort, pagination
- [x] 7.3 Implement IRoleRepository.GetByIdWithPermissionsAsync
- [x] 7.4 Implement IAuditLogRepository.GetPagedAsync with pagination
- [x] 7.5 Implement IRefreshTokenRepository.RevokeAllGlobalAsync
- [x] 7.6 Implement IRefreshTokenRepository.RevokeAllForUserAsync
