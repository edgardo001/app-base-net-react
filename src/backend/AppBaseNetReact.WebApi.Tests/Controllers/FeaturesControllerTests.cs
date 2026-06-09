using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Controllers;

namespace AppBaseNetReact.WebApi.Tests.Controllers;

public class FeaturesControllerTests
{
    [Fact]
    public void GetFeatures_WhenForgotPasswordEnabled_ReturnsTrue()
    {
        var options = Options.Create(new EmailOptions { ForgotPasswordEnabled = true });
        var controller = new FeaturesController(options);

        var result = controller.GetFeatures();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetFeatures_WhenForgotPasswordDisabled_ReturnsFalse()
    {
        var options = Options.Create(new EmailOptions { ForgotPasswordEnabled = false });
        var controller = new FeaturesController(options);

        var result = controller.GetFeatures();

        result.Should().BeOfType<OkObjectResult>();
    }
}
