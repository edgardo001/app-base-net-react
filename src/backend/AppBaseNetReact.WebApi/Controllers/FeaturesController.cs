using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController : ControllerBase
{
    private readonly EmailOptions _emailOptions;
    private readonly TurnstileOptions _turnstileOptions;
    private readonly ICaptchaService _captcha;

    public FeaturesController(
        IOptions<EmailOptions> emailOptions,
        IOptions<TurnstileOptions> turnstileOptions,
        ICaptchaService captcha)
    {
        _emailOptions = emailOptions.Value;
        _turnstileOptions = turnstileOptions.Value;
        _captcha = captcha;
    }

    [HttpGet]
    public IActionResult GetFeatures()
    {
        return Ok(new
        {
            ForgotPasswordEnabled = _emailOptions.ForgotPasswordEnabled,
            CaptchaEnabled = _captcha.IsEnabled,
            CaptchaSiteKey = _captcha.IsEnabled ? _turnstileOptions.SiteKey : null
        });
    }
}
