## ADDED Requirements

### Requirement: View own profile
The authenticated user SHALL view their own profile information.

#### Scenario: Get profile
- **WHEN** GET /api/profile is called with a valid JWT
- **THEN** the user's Id, Email, FirstName, LastName, AvatarPath SHALL be returned
- **THEN** the userId SHALL be extracted from the JWT "sub" claim

#### Scenario: Profile not found
- **WHEN** the user from the JWT does not exist in the database
- **THEN** 404 NotFound SHALL be returned

### Requirement: Update own profile
The authenticated user SHALL update their own first and last name.

#### Scenario: Successful profile update
- **WHEN** PUT /api/profile is called with firstName and lastName
- **THEN** the user's name SHALL be updated
- **THEN** an AuditLog entry SHALL be created with old and new values

### Requirement: View own activity
The authenticated user SHALL view their recent activity log.

#### Scenario: Get activity
- **WHEN** GET /api/profile/activity is called
- **THEN** the last 20 audit log entries for the user SHALL be returned
- **THEN** each entry SHALL include Action, EntityType, Details, CreatedAt
