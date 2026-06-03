## ADDED Requirements

### Requirement: PostgreSQL persistence with EF Core 10
The system SHALL use Entity Framework Core 10 with Npgsql provider connecting to PostgreSQL 18.

#### Scenario: DbContext configuration
- **WHEN** the application starts
- **THEN** AppDbContext SHALL be configured with PostgreSQL connection string from configuration
- **THEN** migrations assembly SHALL be set to Infrastructure assembly

#### Scenario: Entity configurations via IEntityTypeConfiguration
- **WHEN** OnModelCreating is called
- **THEN** all entity configurations from the assembly containing UserConfiguration SHALL be applied

### Requirement: Repository pattern with UnitOfWork
The system SHALL implement generic repository interface `IRepository<T>` and concrete implementations, with `IUnitOfWork` coordinating persistence.

#### Scenario: UnitOfWork commit
- **WHEN** `IUnitOfWork.SaveChangesAsync()` is called
- **THEN** all pending changes SHALL be persisted atomically
- **THEN** Audit fields (UpdatedAt) SHALL be auto-set for modified BaseEntity instances

#### Scenario: Generic repository operations
- **WHEN** adding an entity via `IRepository<T>.AddAsync(entity)`
- **THEN** the entity SHALL be tracked by the DbContext for subsequent SaveChangesAsync

### Requirement: Migration with full schema
The system SHALL have an initial EF Core migration creating all 8 tables with proper relationships and indexes.

#### Scenario: Initial migration tables
- **WHEN** the initial migration is applied
- **THEN** tables Users, Roles, Permissions, RefreshTokens, AuditLogs, LoginAttempts, UserRoles, RolePermissions SHALL be created
- **THEN** unique indexes SHALL exist on User.Email, User.NormalizedEmail, Role.NormalizedName, Permission.Code, RefreshToken.JwtId

#### Scenario: Cascade delete on join tables
- **WHEN** a User or Role is deleted
- **THEN** related UserRole entries SHALL be cascade deleted
