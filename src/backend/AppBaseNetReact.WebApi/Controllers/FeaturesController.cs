using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController : ControllerBase
{
    private readonly EmailOptions _emailOptions;

    public FeaturesController(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    [HttpGet]
    public IActionResult GetFeatures()
    {
        return Ok(new
        {
            ForgotPasswordEnabled = _emailOptions.ForgotPasswordEnabled
        });
    }
}
