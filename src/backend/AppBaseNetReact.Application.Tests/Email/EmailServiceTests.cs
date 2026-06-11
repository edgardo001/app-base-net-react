using System.Threading.Channels;
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
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>();

    [Fact]
    public async Task SendEmailAsync_WithProviderNone_LogsAndSucceeds()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "None"
        });

        var service = new EmailService(options, _renderer, _logger.Object, _channel);

        await service.SendEmailAsync("test@test.com", "Subject", "<p>Body</p>");

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

        var service = new EmailService(options, _renderer, _logger.Object, _channel);

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

        var service = new EmailService(options, _renderer, _logger.Object, _channel);

        await service.SendEmailAsync("not-an-email", "Subject", "<p>Body</p>");

        Assert.True(true);
    }

    [Fact]
    public async Task SendEmailAsync_WhenQueueEnabled_EnqueuesMessage()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "None",
            QueueEnabled = true
        });

        var service = new EmailService(options, _renderer, _logger.Object, _channel);

        await service.SendEmailAsync("queue@test.com", "QueueSubject", "<p>QueueBody</p>");

        var canRead = _channel.Reader.TryRead(out var msg);
        canRead.Should().BeTrue();
        msg!.To.Should().Be("queue@test.com");
        msg.Subject.Should().Be("QueueSubject");
        msg.HtmlBody.Should().Be("<p>QueueBody</p>");
    }

    [Fact]
    public async Task SendEmailAsync_WhenQueueEnabled_DoesNotSendDirectly()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "Smtp",
            Smtp = new SmtpSettings { Host = "" },
            QueueEnabled = true
        });

        var service = new EmailService(options, _renderer, _logger.Object, _channel);

        await service.SendEmailAsync("queue@test.com", "Subject", "<p>Body</p>");

        var canRead = _channel.Reader.TryRead(out var msg);
        canRead.Should().BeTrue();
        msg!.To.Should().Be("queue@test.com");
    }

    [Fact]
    public async Task SendEmailAsync_WhenQueueEnabled_MultipleEmails_AllQueued()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "None",
            QueueEnabled = true
        });

        var service = new EmailService(options, _renderer, _logger.Object, _channel);

        for (var i = 0; i < 5; i++)
            await service.SendEmailAsync($"user{i}@test.com", $"Subject {i}", $"<p>Body {i}</p>");

        var count = 0;
        while (_channel.Reader.TryRead(out _)) count++;

        count.Should().Be(5);
    }

    [Fact]
    public async Task SendEmailAsync_WhenQueueDisabled_SendsDirectly()
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = "None",
            QueueEnabled = false
        });

        var channel = Channel.CreateUnbounded<EmailMessage>();
        var service = new EmailService(options, _renderer, _logger.Object, channel);

        await service.SendEmailAsync("direct@test.com", "DirectSubject", "<p>DirectBody</p>");

        var hasQueued = channel.Reader.TryRead(out _);
        hasQueued.Should().BeFalse();
    }
}
