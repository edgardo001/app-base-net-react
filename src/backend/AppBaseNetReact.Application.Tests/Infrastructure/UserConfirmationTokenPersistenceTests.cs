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
}
