## ADDED Requirements

### Requirement: List users with pagination
The system SHALL provide a paginated, searchable, sortable list of users.

#### Scenario: Default paginated list
- **WHEN** GET /api/users is called
- **THEN** the response SHALL include items, totalCount, page, pageSize, totalPages
- **THEN** default page SHALL be 1, default pageSize SHALL be 10

#### Scenario: Search by email or name
- **WHEN** GET /api/users?search=john is called
- **THEN** users with matching email, firstName, or lastName SHALL be returned

#### Scenario: Sort by column
- **WHEN** GET /api/users?sortBy=email&sortDesc=true is called
- **THEN** results SHALL be sorted by email in descending order

### Requirement: Create user
The system SHALL allow admins to create new users with password hashing and role assignment.

#### Scenario: Successful user creation
- **WHEN** POST /api/users is called with valid email, firstName, lastName, password, and optional roleIds
- **THEN** the user SHALL be created with hashed password
- **THEN** the user SHALL be assigned the specified roles
- **THEN** 201 Created SHALL be returned

### Requirement: Update user
The system SHALL allow admins to update user profile and role assignments.

#### Scenario: Successful user update
- **WHEN** PUT /api/users/{id} is called with firstName, lastName, and roleIds
- **THEN** the user's name SHALL be updated
- **THEN** the user's roles SHALL be replaced with the specified roleIds
- **THEN** 200 OK SHALL be returned

### Requirement: Soft delete user
The system SHALL allow soft deletion of users (DeletedAt set, data preserved).

#### Scenario: Successful user deletion
- **WHEN** DELETE /api/users/{id} is called
- **THEN** the user's DeletedAt SHALL be set (soft delete)
- **THEN** the user SHALL no longer appear in user lists

### Requirement: Toggle user active status
The system SHALL allow admins to activate or deactivate users.

#### Scenario: User deactivation
- **WHEN** PATCH /api/users/{id}/activate with {"active": false}
- **THEN** the user's IsActive SHALL be set to false
- **THEN** the user SHALL not be able to log in

### Requirement: Admin reset password
The system SHALL allow admins to reset a user's password to a temporary one.

#### Scenario: Successful password reset
- **WHEN** PATCH /api/users/{id}/reset-password is called
- **THEN** a new random 12-character password SHALL be generated and hashed
- **THEN** the user's email SHALL be confirmed
- **THEN** the user SHALL be forced to change password on next login
- **THEN** the new temporary password SHALL be returned

### Requirement: Admin revoke user tokens
The system SHALL allow admins to revoke all sessions for a specific user.

#### Scenario: Successful token revocation
- **WHEN** PATCH /api/users/{id}/revoke-tokens is called
- **THEN** ALL refresh tokens for the user SHALL be revoked
