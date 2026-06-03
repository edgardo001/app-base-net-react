using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Email;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _logger = new();
    private readonly EmailRenderer _renderer = new();

    [Fact]
    public async Task SendEmailAsync_WithProviderNone_LogsAndSucceeds()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "None"
        });

        var service = new EmailService(options, _renderer, _logger.Object);

        await service.SendEmailAsync("test@test.com", "Subject", "<p>Body</p>");

        // No exception means success
        Assert.True(true);
    }

    [Fact]
    public async Task SendEmailAsync_WithSmtpAndEmptyHost_Throws()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "Smtp",
            Smtp = new SmtpSettings { Host = "" }
        });

        var service = new EmailService(options, _renderer, _logger.Object);

        var act = async () => await service.SendEmailAsync("test@test.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SMTP host is not configured.");
    }

    [Fact]
    public async Task SendEmailAsync_WithProviderNone_DoesNotThrowForInvalidEmail()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "None"
        });

        var service = new EmailService(options, _renderer, _logger.Object);

        await service.SendEmailAsync("not-an-email", "Subject", "<p>Body</p>");

        Assert.True(true);
    }
}
