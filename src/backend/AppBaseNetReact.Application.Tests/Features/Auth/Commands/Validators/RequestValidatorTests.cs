using FluentAssertions;
using FluentValidation;
using AppBaseNetReact.Application.Common.Validators;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.Validators;

public class RequestValidatorTests
{
    [Fact]
    public void LoginRequestValidator_WithValidData_Passes()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest("test@test.com", "password"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginRequestValidator_WithEmptyEmail_Fails()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest("", "password"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void LoginRequestValidator_WithEmptyPassword_Fails()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest("test@test.com", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void LoginRequestValidator_WithTooLongEmail_Fails()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest(new string('a', 257), "password"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RefreshRequestValidator_WithValidToken_Passes()
    {
        var validator = new RefreshRequestValidator();
        var result = validator.Validate(new RefreshRequest("valid-token"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RefreshRequestValidator_WithEmptyToken_Fails()
    {
        var validator = new RefreshRequestValidator();
        var result = validator.Validate(new RefreshRequest(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChangePasswordRequestValidator_WithValidData_Passes()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.Validate(new ChangePasswordRequest("current", "NewPassword1!", "NewPassword1!"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangePasswordRequestValidator_WithMismatchedPasswords_Fails()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.Validate(new ChangePasswordRequest("current", "NewPassword1!", "Different1!"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
    }

    [Fact]
    public void ChangePasswordRequestValidator_WithShortNewPassword_Fails()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.Validate(new ChangePasswordRequest("current", "abc", "abc"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void ChangePasswordRequestValidator_WithEmptyCurrentPassword_Fails()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.Validate(new ChangePasswordRequest("", "NewPassword1!", "NewPassword1!"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
    }

    [Fact]
    public void ResetPasswordRequestValidator_WithValidData_Passes()
    {
        var validator = new ResetPasswordRequestValidator();
        var result = validator.Validate(new ResetPasswordRequest("token", "NewPassword1!", "NewPassword1!"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResetPasswordRequestValidator_WithEmptyToken_Fails()
    {
        var validator = new ResetPasswordRequestValidator();
        var result = validator.Validate(new ResetPasswordRequest("", "NewPassword1!", "NewPassword1!"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordRequestValidator_WithMismatchedPasswords_Fails()
    {
        var validator = new ResetPasswordRequestValidator();
        var result = validator.Validate(new ResetPasswordRequest("token", "NewPassword1!", "Different1!"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfirmEmailRequestValidator_WithValidToken_Passes()
    {
        var validator = new ConfirmEmailRequestValidator();
        var result = validator.Validate(new ConfirmEmailRequest("valid-token"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConfirmEmailRequestValidator_WithEmptyToken_Fails()
    {
        var validator = new ConfirmEmailRequestValidator();
        var result = validator.Validate(new ConfirmEmailRequest(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateUserRequestValidator_WithValidData_Passes()
    {
        var validator = new CreateUserRequestValidator();
        var result = validator.Validate(new CreateUserRequest("test@test.com", "Test", "User", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateUserRequestValidator_WithInvalidEmail_Fails()
    {
        var validator = new CreateUserRequestValidator();
        var result = validator.Validate(new CreateUserRequest("not-an-email", "Test", "User", null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateUserRequestValidator_WithEmptyFirstName_Fails()
    {
        var validator = new CreateUserRequestValidator();
        var result = validator.Validate(new CreateUserRequest("test@test.com", "", "User", null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateUserRequestValidator_WithEmptyLastName_Fails()
    {
        var validator = new CreateUserRequestValidator();
        var result = validator.Validate(new CreateUserRequest("test@test.com", "Test", "", null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateUserRequestValidator_WithValidData_Passes()
    {
        var validator = new UpdateUserRequestValidator();
        var result = validator.Validate(new UpdateUserRequest("Test", "User", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateUserRequestValidator_WithEmptyFirstName_Fails()
    {
        var validator = new UpdateUserRequestValidator();
        var result = validator.Validate(new UpdateUserRequest("", "User", null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateRoleRequestValidator_WithValidData_Passes()
    {
        var validator = new CreateRoleRequestValidator();
        var result = validator.Validate(new CreateRoleRequest("Admin", "Administrator"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateRoleRequestValidator_WithEmptyName_Fails()
    {
        var validator = new CreateRoleRequestValidator();
        var result = validator.Validate(new CreateRoleRequest("", "desc"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateRoleRequestValidator_WithValidData_Passes()
    {
        var validator = new UpdateRoleRequestValidator();
        var result = validator.Validate(new UpdateRoleRequest("Admin", "desc"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateRoleRequestValidator_WithEmptyName_Fails()
    {
        var validator = new UpdateRoleRequestValidator();
        var result = validator.Validate(new UpdateRoleRequest("", "desc"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdatePermissionsRequestValidator_WithValidData_Passes()
    {
        var validator = new UpdatePermissionsRequestValidator();
        var result = validator.Validate(new UpdatePermissionsRequest(
            new List<PermissionAssignment> { new(Guid.NewGuid(), true) }));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdatePermissionsRequestValidator_WithEmptyList_Fails()
    {
        var validator = new UpdatePermissionsRequestValidator();
        var result = validator.Validate(new UpdatePermissionsRequest(
            new List<PermissionAssignment>()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdatePermissionsRequestValidator_WithEmptyPermissionId_Fails()
    {
        var validator = new UpdatePermissionsRequestValidator();
        var result = validator.Validate(new UpdatePermissionsRequest(
            new List<PermissionAssignment> { new(Guid.Empty, true) }));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProfileRequestValidator_WithValidData_Passes()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.Validate(new UpdateProfileRequest("Test", "User"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateProfileRequestValidator_WithEmptyFirstName_Fails()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.Validate(new UpdateProfileRequest("", "User"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProfileRequestValidator_WithTooLongLastName_Fails()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.Validate(new UpdateProfileRequest("Test", new string('a', 101)));

        result.IsValid.Should().BeFalse();
    }
}
