using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Controllers;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly EmailRenderer _renderer = new();
    private readonly EmailOptions _emailOptions = new()
    {
        FrontendBaseUrl = "https://app",
        Templates = new Dictionary<string, EmailTemplateConfig>
        {
            ["TestEmail"] = new() { Subject = "Test", TemplateFile = "test-email.html" }
        }
    };
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _controller = new AdminController(
            _uow.Object, _audit.Object, _email.Object, _renderer, Options.Create(_emailOptions));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Scheme = "https", Host = new HostString("app.example.com") }
            }
        };
    }

    [Fact]
    public void SendTestEmail_InheritsClassLevelAuthorizeForAdminAndSuperAdmin()
    {
        // Regression guard: Admin and SuperAdmin roles may access all AdminController
        // endpoints. The class-level [Authorize(Roles = "SuperAdmin,Admin")] covers
        // all actions including test-email. This test documents the rule at the
        // metadata level because the actual authorization evaluation happens in the
        // ASP.NET Core pipeline, not in the controller's method body.
        var method = typeof(AdminController).GetMethod(
            nameof(AdminController.SendTestEmail),
            BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull("SendTestEmail action must exist on AdminController");

        var methodAuth = method!.GetCustomAttribute<AuthorizeAttribute>();
        methodAuth.Should().BeNull(
            "test-email no longer needs an explicit Authorize attribute since class-level already allows Admin and SuperAdmin");
    }

    [Fact]
    public void AdminController_ClassLevelAuthorize_AllowsAdminAndSuperAdmin()
    {
        // Documents the class-level authorization: both Admin and SuperAdmin
        // roles may access all endpoints in this controller (dashboard,
        // audit-log, revoke-all-tokens, test-email).
        var classAuth = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();
        classAuth.Should().NotBeNull();
        var roles = classAuth!.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        roles.Should().Contain("SuperAdmin",
            "SuperAdmin keeps the existing super-user privilege");
        roles.Should().Contain("Admin",
            "Admin must be allowed to access dashboard, audit-log, and test-email");
        roles.Should().NotContain(r => r != "SuperAdmin" && r != "Admin",
            "no other roles may access admin endpoints (regression guard)");
    }

    [Fact]
    public async Task SendTestEmail_WithTemplateAndSmtpConfigured_Returns200AndSends()
    {
        // Smoke test for the happy path of the action body (post-authorization).
        // Verifies the audit log is written and the email service is invoked.
        _email.Setup(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.SendTestEmail(
            new SendTestEmailRequest("u@test.com"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _email.Verify(x => x.SendEmailAsync(
            "u@test.com",
            "Test",
            It.Is<string>(html => html.Contains("u@test.com") == false && html.Contains("app.example.com") == false),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _audit.Verify(x => x.LogAsync(
            "TestEmailSent",
            "Email",
            null,
            $"Test email sent to u@test.com",
            null,
            null,
            It.IsAny<string>(),
            It.IsAny<string>(),
            null,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendTestEmail_WhenTemplateMissing_Returns400()
    {
        // Removes the TestEmail template from options to verify the
        // "template not configured" branch.
        var emptyOptions = new EmailOptions
        {
            FrontendBaseUrl = "https://app",
            Templates = new Dictionary<string, EmailTemplateConfig>() // no TestEmail
        };
        var controller = new AdminController(
            _uow.Object, _audit.Object, _email.Object, _renderer, Options.Create(emptyOptions));
        controller.ControllerContext = _controller.ControllerContext;

        var result = await controller.SendTestEmail(
            new SendTestEmailRequest("u@test.com"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _email.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "missing template must abort before invoking the email service");
    }
}
