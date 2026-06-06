using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence;
using AppBaseNetReact.Infrastructure.Persistence.Repositories;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Infrastructure;

// Integration test: exercises the FULL user-creation → token-lookup
// flow against a real AppDbContext (EF Core InMemory provider) to catch
// any persistence-layer bug that unit tests with mocked IUnitOfWork
// would miss. Specifically, this guards against a regression where
// the EmailConfirmationToken is generated client-side but not actually
// written to the database (so a subsequent GetByEmailConfirmationTokenAsync
// returns null and the user sees "Invalid confirmation token").
public class UserConfirmationTokenPersistenceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Token_SetOnNewUser_IsPersistedAndFoundByLookup()
    {
        // Arrange: real DbContext, real UnitOfWork, real UserRepository.
        // The only mock would be the IEmailService; here we don't even
        // need that — we're just exercising the write path.
        var context = CreateContext(nameof(Token_SetOnNewUser_IsPersistedAndFoundByLookup));
        var uow = new UnitOfWork(context);

        var user = User.Create(
            "newuser@test.com",
            "New",
            "User",
            passwordHash: "irrelevant-hash");

        // Act: generate the same kind of token the controller generates,
        // set it on the entity, persist via the real repository.
        var confirmationToken = Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        user.SetEmailConfirmationToken(confirmationToken, DateTime.UtcNow.AddHours(24));

        await uow.Users.AddAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        // Detach so the next query goes back to storage (mimics a real
        // request where the request scope ends and a new one begins).
        context.ChangeTracker.Clear();

        // Assert: the token lookup that ConfirmEmailCommandHandler performs
        // MUST return the same user.
        var found = await uow.Users.GetByEmailConfirmationTokenAsync(
            confirmationToken, CancellationToken.None);

        found.Should().NotBeNull(
            "the token generated and persisted in CreateUser must be findable by " +
            "GetByEmailConfirmationTokenAsync; if this fails, the token is being " +
            "set on the in-memory entity but not flushed to storage, or the global " +
            "query filter is hiding the row");
        found!.Id.Should().Be(user.Id);
        found.EmailConfirmationToken.Should().Be(confirmationToken);
        found.EmailConfirmationTokenExpires.Should().NotBeNull();
        found.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Token_LookupWithDifferentCasing_ReturnsNull()
    {
        // Documents the case-sensitivity contract: Convert.ToHexString
        // produces uppercase, the column is compared case-sensitively
        // (PostgreSQL default), and a lowercase query string from a
        // client that URL-decoded or transformed the token would fail.
        // This is informational; the frontend uses useSearchParams which
        // preserves the original case.
        var context = CreateContext(nameof(Token_LookupWithDifferentCasing_ReturnsNull));
        var uow = new UnitOfWork(context);

        var user = User.Create("a@test.com", "A", "U", "h");
        var token = "ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890";
        user.SetEmailConfirmationToken(token, DateTime.UtcNow.AddHours(24));
        await uow.Users.AddAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var lower = await uow.Users.GetByEmailConfirmationTokenAsync(
            token.ToLowerInvariant(), CancellationToken.None);
        lower.Should().BeNull(
            "the comparison is case-sensitive; tokens are stored uppercase");
    }

    [Fact]
    public async Task Token_AfterEmailConfirmed_IsNulledAndLookupReturnsNull()
    {
        // Documents the ConfirmEmail() side-effect: token + expiry are
        // cleared, so the same token cannot be reused. A test of the
        // happy path of ConfirmEmailCommandHandler belongs in the
        // handler unit tests; this only verifies the entity invariant.
        var context = CreateContext(nameof(Token_AfterEmailConfirmed_IsNulledAndLookupReturnsNull));
        var uow = new UnitOfWork(context);

        var user = User.Create("b@test.com", "B", "U", "h");
        var token = "TOKENAFTERCONFIRM1234567890ABCDEF1234567890ABCDEF1234567890AB";
        user.SetEmailConfirmationToken(token, DateTime.UtcNow.AddHours(24));
        await uow.Users.AddAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        user.ConfirmEmail();
        await uow.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        user.EmailConfirmed.Should().BeTrue();
        user.EmailConfirmationToken.Should().BeNull();
        var found = await uow.Users.GetByEmailConfirmationTokenAsync(
            token, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact]
    public async Task Token_AfterForcePasswordChange_StillAllowsConfirmation()
    {
        // Guard against regression: UsersController.CreateUser now calls
        // ForcePasswordChange() so the user is redirected to
        // /change-password on first login. The EmailConfirmationToken
        // must remain intact and findable, otherwise the user can never
        // confirm their email and the whole onboarding flow breaks.
        var context = CreateContext(nameof(Token_AfterForcePasswordChange_StillAllowsConfirmation));
        var uow = new UnitOfWork(context);

        var user = User.Create("c@test.com", "C", "U", "h");
        var token = "TOKENAFTERFORCECHANGE1234567890ABCDEF1234567890ABCDEF12345AB";
        user.SetEmailConfirmationToken(token, DateTime.UtcNow.AddHours(24));
        await uow.Users.AddAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        user.ForcePasswordChange();
        await uow.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        user.LastPasswordChangeAt.Should().BeNull(
            "ForcePasswordChange must set LastPasswordChangeAt = null so IsPasswordExpired returns true");
        var found = await uow.Users.GetByEmailConfirmationTokenAsync(
            token, CancellationToken.None);
        found.Should().NotBeNull(
            "the confirmation token must survive ForcePasswordChange; otherwise a user created with a " +
            "temporary password would never be able to confirm their email and would be locked out");
        found!.Id.Should().Be(user.Id);
        found.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task GetByEmailAsync_ForSoftDeletedUser_ReturnsNull()
    {
        // Documents the contract: GetByEmailAsync respects the global
        // query filter (DeletedAt == null), so it returns null for a
        // soft-deleted user. This is the EXACT precondition for the
        // duplicate-email bug: a user with email X is soft-deleted,
        // a new user with email X is requested, GetByEmailAsync returns
        // null, the controller proceeds with the insert, and PostgreSQL
        // rejects it with IX_Users_Email. The fix is to make the unique
        // index partial (WHERE "DeletedAt" IS NULL) so the DB permits
        // the reuse; see migration 20260606_PartialUniqueEmailForActiveUsers.
        var context = CreateContext(nameof(GetByEmailAsync_ForSoftDeletedUser_ReturnsNull));
        var uow = new UnitOfWork(context);

        var user = User.Create("ghost@test.com", "G", "U", "h");
        await uow.Users.AddAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        // Sanity check: while the user is active, the lookup finds them.
        (await uow.Users.GetByEmailAsync("ghost@test.com", CancellationToken.None))
            .Should().NotBeNull();

        // Soft-delete via the repository (sets DeletedAt).
        await uow.Users.DeleteAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        // After soft-delete, the lookup returns null because of the
        // global query filter on User.DeletedAt == null. This is the
        // precondition that lets the controller's pre-check pass even
        // though the email is still occupied in the Users table.
        (await uow.Users.GetByEmailAsync("ghost@test.com", CancellationToken.None))
            .Should().BeNull(
                "GetByEmailAsync filters by DeletedAt == null; this is the soft-delete invariant " +
                "that makes a partial unique index on (Email) WHERE DeletedAt IS NULL necessary " +
                "for email reuse after deactivation");
    }

    [Fact]
    public async Task CreateUser_PassesPreCheck_WhenOnlySoftDeletedUserHasEmail()
    {
        // Documents the gap in the controller's pre-check: with a real
        // soft-deleted user in the table, the controller's call to
        // GetByEmailAsync returns null (query filter), the 409 branch
        // is skipped, and the controller proceeds with AddAsync +
        // SaveChangesAsync. Against PostgreSQL with a non-partial
        // unique index, the SaveChangesAsync throws DbUpdateException
        // (IX_Users_Email violation). The InMemory provider does NOT
        // enforce unique constraints, so this test verifies the
        // application-layer behavior; the DB-layer fix lives in the
        // partial unique index migration.
        var context = CreateContext(nameof(CreateUser_PassesPreCheck_WhenOnlySoftDeletedUserHasEmail));
        var uow = new UnitOfWork(context);

        var taken = User.Create("reused@test.com", "Old", "User", "h");
        await uow.Users.AddAsync(taken, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        await uow.Users.DeleteAsync(taken, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        // GetByEmailAsync returns null (soft-deleted -> query filter).
        var existing = await uow.Users.GetByEmailAsync("reused@test.com", CancellationToken.None);
        existing.Should().BeNull(
            "precondition: the soft-deleted user must not be visible to GetByEmailAsync " +
            "for this test to model the production bug");

        // The controller, given this null, will call AddAsync + SaveChangesAsync.
        // Against PostgreSQL with a non-partial unique index, SaveChangesAsync throws.
        // Against PostgreSQL with a partial unique index, SaveChangesAsync succeeds.
        // (This test uses InMemory which never enforces unique constraints, so we only
        //  assert the application-layer behavior; the migration is the actual fix.)
        var newUser = User.Create("reused@test.com", "New", "User", "h");
        await uow.Users.AddAsync(newUser, CancellationToken.None);
        var act = async () => await uow.SaveChangesAsync(CancellationToken.None);

        // InMemory: succeeds (the test asserts what the controller *would do*).
        // PostgreSQL pre-fix:  would throw Npgsql.PostgresException 23505 IX_Users_Email.
        // PostgreSQL post-fix: succeeds because the unique index is partial.
        await act.Should().NotThrowAsync(
            "InMemory provider; on PostgreSQL this would fail pre-migration and pass post-migration");
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUserWithRolesAndPermissionsLoaded()
    {
        // Regression guard for the production bug where the JWT issued
        // at login had no role or permission claims. Cause:
        // GetByEmailAsync did not Include UserRoles (or its navigations),
        // so the navigation collection was empty when JwtService iterated
        // user.UserRoles to emit role/permission claims. The fix adds
        // Include(UserRoles).ThenInclude(Role).ThenInclude(RolePermissions)
        // .ThenInclude(Permission) so the LoginCommandHandler can build
        // a complete JWT.
        //
        // The symptom was a 403 on every [Authorize(Roles = ...)] endpoint
        // for every user, and an empty roles array in the frontend
        // (test-email card hidden for everyone). Detaching via
        // ChangeTracker.Clear() forces a fresh load, which is the only
        // way the test catches a missing Include: without it, the
        // InMemory provider returns the tracked entity and the
        // navigation property is "magically" populated.
        var context = CreateContext(nameof(GetByEmailAsync_ReturnsUserWithRolesAndPermissionsLoaded));
        var uow = new UnitOfWork(context);

        var role = Role.Create("Admin", "Admin role");
        var permission = Permission.Create("admin:dashboard", "Dashboard", "Admin", "view dashboard");
        context.Roles.Add(role);
        context.Permissions.Add(permission);
        await context.SaveChangesAsync();
        role.RolePermissions.Add(RolePermission.Create(role.Id, permission.Id));

        var user = User.Create("u@test.com", "Test", "User", "h");
        user.UserRoles.Add(UserRole.Create(user.Id, role.Id));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Detach so the next query has to reload from the store.
        context.ChangeTracker.Clear();

        var found = await uow.Users.GetByEmailAsync("u@test.com", CancellationToken.None);

        found.Should().NotBeNull();
        found!.UserRoles.Should().NotBeNull().And.NotBeEmpty(
            "GetByEmailAsync MUST Include UserRoles; otherwise the JWT issued at login " +
            "has no role claims and [Authorize(Roles = ...)] rejects every user");
        var userRole = found.UserRoles.First();
        userRole.Role.Should().NotBeNull(
            "GetByEmailAsync MUST Include(UserRoles).ThenInclude(Role); without it, " +
            "JwtService throws NullReferenceException when reading userRole.Role.Name");
        userRole.Role!.RolePermissions.Should().NotBeEmpty(
            "GetByEmailAsync MUST Include RolePermissions; without it, the JWT has no " +
            "permission claims and permission-based [Authorize] rejects every user");
        userRole.Role.RolePermissions.First().Permission.Should().NotBeNull();
    }
}
