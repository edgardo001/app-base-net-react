using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Controllers;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class FeaturesControllerTests
{
    private readonly Mock<ICaptchaService> _captcha = new();
    private readonly TurnstileOptions _turnstileOptions = new();

    [Fact]
    public void GetFeatures_WhenForgotPasswordEnabled_ReturnsOk()
    {
        var emailOptions = Options.Create(new EmailOptions { ForgotPasswordEnabled = true });
        var controller = new FeaturesController(emailOptions, Options.Create(_turnstileOptions), _captcha.Object);

        var result = controller.GetFeatures();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void GetFeatures_WhenForgotPasswordDisabled_ReturnsOk()
    {
        var emailOptions = Options.Create(new EmailOptions { ForgotPasswordEnabled = false });
        var controller = new FeaturesController(emailOptions, Options.Create(_turnstileOptions), _captcha.Object);

        var result = controller.GetFeatures();

        result.Should().BeOfType<OkObjectResult>();
    }
}
