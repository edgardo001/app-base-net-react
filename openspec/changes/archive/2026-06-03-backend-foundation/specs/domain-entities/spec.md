## ADDED Requirements

### Requirement: Domain entities with encapsulated behavior
The system SHALL implement rich domain model entities with behavior encapsulation (private setters, factory methods).

#### Scenario: User entity creation
- **WHEN** `User.Create(email, firstName, lastName, passwordHash, createdBy)` is called
- **THEN** a new User instance SHALL be created with Id (Guid), Email, NormalizedEmail (uppercase), PasswordHash, FirstName, LastName, SecurityStamp, CreatedAt (UTC), LastPasswordChangeAt (UTC)
- **THEN** the User SHALL have IsActive = true, LockoutEnabled = true, PasswordExpirationDays = 30

#### Scenario: User profile update
- **WHEN** `user.UpdateProfile(firstName, lastName)` is called
- **THEN** FirstName and LastName SHALL be updated, UpdatedAt SHALL be set to current UTC time

#### Scenario: User password change
- **WHEN** `user.SetPasswordHash(newHash)` is called
- **THEN** PasswordHash SHALL be updated and SecurityStamp SHALL be regenerated (new GUID)
- **THEN** LastPasswordChangeAt SHALL be set to current UTC time

#### Scenario: User failed login tracking
- **WHEN** `user.IncrementFailedAccess()` is called
- **THEN** AccessFailedCount SHALL increment by 1

#### Scenario: User account lockout
- **WHEN** `user.LockUntil(until)` is called
- **THEN** LockoutEnd SHALL be set to the specified DateTime

#### Scenario: User active status toggle
- **WHEN** `user.SetActive(false, updatedBy)` is called
- **THEN** IsActive SHALL be set to false, UpdatedAt and UpdatedBy SHALL be set

#### Scenario: Soft delete
- **WHEN** `entity.SoftDelete(deletedBy)` is called on any BaseEntity
- **THEN** DeletedAt SHALL be set to current UTC time, UpdatedAt and UpdatedBy SHALL be set

### Requirement: Role entity with system protection
The system SHALL implement Role entity with IsSystem flag preventing deletion of critical roles.

#### Scenario: System role creation
- **WHEN** `Role.Create(name, description, isSystem: true)` is called
- **THEN** the Role SHALL be created with IsSystem = true

#### Scenario: System role update blocked
- **WHEN** attempting to update or delete a system role
- **THEN** the operation SHALL be blocked at the application layer

### Requirement: RefreshToken with rotation and reuse detection
The system SHALL implement RefreshToken entity supporting rotation (replace old token hash) and reuse detection (revoke all user tokens if a revoked token is reused).

#### Scenario: Token creation
- **WHEN** `RefreshToken.Create(userId, jwtId, tokenHash, expiresAt, deviceInfo, ipAddress)` is called
- **THEN** the token SHALL be stored with the hashed value, not the plain token

#### Scenario: Token rotation
- **WHEN** a token is rotated via `token.Revoke(revokedBy, replacedByTokenHash)`
- **THEN** RevokedAt, RevokedBy and ReplacedByTokenHash SHALL be set
- **THEN** `IsRevoked` SHALL return true

#### Scenario: Token expiration check
- **WHEN** `token.IsExpired` is evaluated after ExpiresAt has passed
- **THEN** it SHALL return true

### Requirement: BaseEntity with audit and concurrency
All entities SHALL inherit from BaseEntity providing GUID PK, audit fields (CreatedAt/CreatedBy, UpdatedAt/UpdatedBy), soft delete (DeletedAt), and optimistic concurrency (ConcurrencyToken byte array).

#### Scenario: Concurrency conflict detection
- **WHEN** two users attempt to update the same entity concurrently
- **THEN** EF Core SHALL throw DbUpdateConcurrencyException on the second save

#### Scenario: Soft delete global filter
- **WHEN** querying entities from the database
- **THEN** entities with non-null DeletedAt SHALL be excluded by default via global query filter
