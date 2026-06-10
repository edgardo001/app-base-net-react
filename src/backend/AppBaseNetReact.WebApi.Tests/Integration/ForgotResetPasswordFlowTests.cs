using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;
using AppBaseNetReact.Application.Features.Auth.Commands.ResetPassword;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.WebApi.Tests.Integration;

[Collection("Integration")]
public class ForgotResetPasswordFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ForgotResetPasswordFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_ThenResetPassword_ChangesPasswordSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var testEmail = $"test-{Guid.NewGuid()}@example.com";
        var originalPassword = "OriginalPass123!";
        var newPassword = "NewSecurePass456!";

        var user = User.Create(testEmail, "Test", "User", hasher.HashPassword(originalPassword));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act - Step 1: Forgot Password
        var forgotCommand = new ForgotPasswordCommand(
            testEmail,
            "127.0.0.1",
            "TestAgent",
            "http://localhost:5173");

        var forgotOutcome = await mediator.Send(forgotCommand);

        // Assert - Forgot Password succeeded
        forgotOutcome.Result.IsSuccess.Should().BeTrue();

        // Act - Step 2: Get reset token from database
        var userFromDb = await dbContext.Users.FirstAsync(u => u.Email == testEmail);
        userFromDb.EmailConfirmationToken.Should().NotBeNull();
        userFromDb.EmailConfirmationTokenExpires.Should().NotBeNull();
        userFromDb.EmailConfirmationTokenExpires!.Value.Should().BeAfter(DateTime.UtcNow);

        var resetToken = userFromDb.EmailConfirmationToken!;

        // Act - Step 3: Reset Password
        var resetCommand = new ResetPasswordCommand(
            resetToken,
            newPassword,
            "127.0.0.1",
            "TestAgent");

        var resetOutcome = await mediator.Send(resetCommand);

        // Assert - Reset Password succeeded
        resetOutcome.Result.IsSuccess.Should().BeTrue();

        // Act - Step 4: Verify password was changed
        var updatedUser = await dbContext.Users.FirstAsync(u => u.Email == testEmail);
        updatedUser.EmailConfirmationToken.Should().BeNull();
        updatedUser.EmailConfirmationTokenExpires.Should().BeNull();

        // Verify old password doesn't work
        hasher.VerifyPassword(originalPassword, updatedUser.PasswordHash).Should().BeFalse();

        // Verify new password works
        hasher.VerifyPassword(newPassword, updatedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ReturnsFailure()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var testEmail = $"test-{Guid.NewGuid()}@example.com";
        var user = User.Create(testEmail, "Test", "User", hasher.HashPassword("SomePass123!"));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Set an expired token
        user.SetEmailConfirmationToken("expired-token-123", DateTime.UtcNow.AddHours(-1));
        await dbContext.SaveChangesAsync();

        // Act
        var resetCommand = new ResetPasswordCommand(
            "expired-token-123",
            "NewPass456!",
            "127.0.0.1",
            "TestAgent");

        var outcome = await mediator.Send(resetCommand);

        // Assert
        outcome.Result.IsSuccess.Should().BeFalse();
        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.ResetTokenExpired);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var resetCommand = new ResetPasswordCommand(
            "non-existent-token",
            "NewPass456!",
            "127.0.0.1",
            "TestAgent");

        var outcome = await mediator.Send(resetCommand);

        // Assert
        outcome.Result.IsSuccess.Should().BeFalse();
        outcome.Result.ErrorCode.Should().Be(PasswordErrorCode.InvalidResetToken);
    }
}
