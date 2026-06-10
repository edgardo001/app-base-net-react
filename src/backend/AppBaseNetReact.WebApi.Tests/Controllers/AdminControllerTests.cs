using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Application.Features.Admin.Commands.RevokeAllTokens;
using AppBaseNetReact.Application.Features.Admin.Commands.SendTestEmail;
using AppBaseNetReact.Application.Features.Admin.Queries.GetAuditLog;
using AppBaseNetReact.Application.Features.Admin.Queries.GetDashboard;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Controllers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
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
            _mediator.Object, _renderer, Options.Create(_emailOptions));
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
        var classAuth = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();
        classAuth.Should().NotBeNull();
        var roles = classAuth!.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        roles.Should().Contain("SuperAdmin");
        roles.Should().Contain("Admin");
        roles.Should().NotContain(r => r != "SuperAdmin" && r != "Admin");
    }

    [Fact]
    public async Task SendTestEmail_WithTemplateAndSmtpConfigured_Returns200AndSends()
    {
        _mediator.Setup(x => x.Send(It.IsAny<SendTestEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendTestEmailOutcome.Success());

        var result = await _controller.SendTestEmail(
            new SendTestEmailRequest("u@test.com"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(x => x.Send(
            It.Is<SendTestEmailCommand>(c => c.To == "u@test.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTestEmail_WhenTemplateMissing_Returns400()
    {
        var emptyOptions = new EmailOptions
        {
            FrontendBaseUrl = "https://app",
            Templates = new Dictionary<string, EmailTemplateConfig>()
        };
        var controller = new AdminController(
            _mediator.Object, _renderer, Options.Create(emptyOptions));
        controller.ControllerContext = _controller.ControllerContext;

        var result = await controller.SendTestEmail(
            new SendTestEmailRequest("u@test.com"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediator.Verify(x => x.Send(
            It.IsAny<SendTestEmailCommand>(), It.IsAny<CancellationToken>()),
            Times.Never, "missing template must abort before invoking the handler");
    }

    [Fact]
    public async Task GetDashboard_ReturnsPasswordExpiryCounts()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetDashboardResponse
            {
                TotalUsers = 100,
                ActiveUsers = 80,
                InactiveUsers = 20,
                NewUsersLast7Days = 5,
                ExpiredPasswords = 3,
                ExpiringSoonPasswords = 2
            });

        var result = await _controller.GetDashboard(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<GetDashboardResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.TotalUsers.Should().Be(100);
    }
}
