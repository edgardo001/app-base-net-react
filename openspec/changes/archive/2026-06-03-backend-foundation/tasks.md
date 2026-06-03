## 1. Solution Structure

- [x] 1.1 Create `AppBaseNetReact.slnx` solution file with 4 projects: Domain, Application, Infrastructure, WebApi
- [x] 1.2 Configure project references: Domain → no deps, Application → Domain, Infrastructure → Application+Domain, WebApi → Application+Infrastructure
- [x] 1.3 Add NuGet packages: Npgsql, Serilog, MediatR, FluentValidation, AutoMapper, Quartz.NET, MailKit, JwtBearer

## 2. Domain Entities

- [x] 2.1 Implement `BaseEntity` abstract class with Id (Guid), audit fields, soft delete, concurrency token
- [x] 2.2 Implement `User` entity with domain methods: Create, UpdateProfile, SetPasswordHash, MarkLogin, IncrementFailedAccess, Lock, Unlock, SetActive, ConfirmEmail, ForcePasswordChange
- [x] 2.3 Implement `Role` entity with IsSystem flag, Create and Update methods
- [x] 2.4 Implement `Permission` entity with Code, Name, Module, Description
- [x] 2.5 Implement `RefreshToken` entity with rotation/reuse detection support (TokenHash, ReplacedByTokenHash, RevokedAt)
- [x] 2.6 Implement `AuditLog` entity for immutable audit trail (Action, EntityType, OldValues, NewValues, IpAddress)
- [x] 2.7 Implement `LoginAttempt` entity for login attempt tracking
- [x] 2.8 Implement `UserRole` and `RolePermission` join entities for many-to-many relationships

## 3. Data Persistence

- [x] 3.1 Configure `AppDbContext` with 8 DbSets and ApplyConfigurationsFromAssembly
- [x] 3.2 Implement entity type configurations (UserConfiguration, RoleConfiguration, etc.)
- [x] 3.3 Configure PostgreSQL connection via Npgsql with migrations assembly
- [x] 3.4 Implement `IRepository<T>` generic interface and concrete `Repository<T>`
- [x] 3.5 Implement `IUnitOfWork` with SaveChangesAsync and repository access
- [x] 3.6 Implement `IUserRepository` with specialized queries (GetByEmail, GetPagedAsync)
- [x] 3.7 Implement `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenRepository`, `IAuditLogRepository`, `ILoginAttemptRepository`
- [x] 3.8 Create initial EF Core migration `InitialCreate` with all 8 tables, indexes, and foreign keys

## 4. Application Layer Infrastructure

- [x] 4.1 Register MediatR with assembly scanning and ValidationBehavior pipeline
- [x] 4.2 Register FluentValidation validators from assembly
- [x] 4.3 Register AutoMapper with profiles from assembly
- [x] 4.4 Define interface contracts: IJwtService, IPasswordHasherService, IEmailService, ICaptchaService, IDateTimeProvider, IAuditService, IPasswordPolicyService
- [x] 4.5 Implement request DTOs with FluentValidation validators (LoginRequest, CreateUserRequest, etc.)

## 5. Logging and Middleware

- [x] 5.1 Configure Serilog with Console and File sinks, bootstrap logger
- [x] 5.2 Implement `ExceptionHandlingMiddleware` for global error handling
- [x] 5.3 Implement `SecurityHeadersMiddleware` with security headers (X-Frame-Options, CSP, etc.)
- [x] 5.4 Configure middleware pipeline: ExceptionHandling → SecurityHeaders → CORS → RateLimiter → Auth → Authorization
- [x] 5.5 Configure CORS with permissive development policy

## 6. Database Seeding

- [x] 6.1 Implement `DatabaseSeeder` with auto-migration on startup
- [x] 6.2 Seed 5 roles: SuperAdmin, Admin, user-tipo-a, user-tipo-b, user-tipo-c
- [x] 6.3 Seed 18 permissions across modules: Users, Roles, Permissions, Audit, Admin, Profile
- [x] 6.4 Seed default SuperAdmin user with credentials admin/admin (forced password change)
- [x] 6.5 Assign permissions to roles according to planInicial.ia.md default permissions

## 7. Container Infrastructure

- [x] 7.1 Create multi-stage Dockerfile.backend (SDK 10 build → ASP.NET 10 runtime)
- [x] 7.2 Create multi-stage Dockerfile.frontend (Node 22 build → Nginx 1.27)
- [x] 7.3 Create nginx.conf with SPA routing, API proxy, static caching, security headers
- [x] 7.4 Create docker-compose.yml with PostgreSQL 18, backend, frontend, Traefik services
- [x] 7.5 Configure environment variables via .env file pattern

## 8. Configuration

- [x] 8.1 Create appsettings.json with all configuration sections (ConnectionStrings, Jwt, PasswordPolicy, Captcha, RateLimiting, Email, Cors, Storage, Session)
- [x] 8.2 Create appsettings.Development.json with relaxed settings for development (lenient password policy, higher rate limits, email disabled)
- [x] 8.3 Create .env.template with required environment variables
