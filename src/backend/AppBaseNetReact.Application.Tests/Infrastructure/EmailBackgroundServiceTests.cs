using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Infrastructure;

public class EmailBackgroundServiceTests
{
    private readonly IOptions<EmailOptions> _options = Options.Create(new EmailOptions
    {
        Provider = "None"
    });

    private static (EmailBackgroundService service, IServiceProvider sp) CreateService(
        IOptions<EmailOptions> options, Channel<EmailMessage> channel, ILogger<EmailBackgroundService>? logger = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(new EmailRenderer());
        services.AddSingleton(Mock.Of<ILogger<EmailService>>());
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        services.AddSingleton<EmailService>();
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var service = new EmailBackgroundService(channel, scopeFactory, options,
            logger ?? Mock.Of<ILogger<EmailBackgroundService>>());
        return (service, sp);
    }

    [Fact]
    public async Task ExecuteAsync_WhenChannelEmpty_DoesNotThrow()
    {
        var channel = Channel.CreateUnbounded<EmailMessage>();
        var (service, _) = CreateService(_options, channel);

        channel.Writer.TryComplete();

        await service.StartAsync(default);
        await Task.Delay(200);
        await service.StopAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_StopsGracefully()
    {
        var channel = Channel.CreateUnbounded<EmailMessage>();
        var (service, _) = CreateService(_options, channel);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await service.StartAsync(cts.Token);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_LogsWarningAndErrorOnFailure()
    {
        var badOptions = Options.Create(new EmailOptions
        {
            Provider = "Smtp",
            Smtp = new SmtpSettings { Host = "" }
        });

        var channel = Channel.CreateUnbounded<EmailMessage>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        var loggerMock = new Mock<ILogger<EmailBackgroundService>>();

        var services = new ServiceCollection();
        services.AddSingleton(badOptions);
        services.AddSingleton(new EmailRenderer());
        services.AddSingleton(Mock.Of<ILogger<EmailService>>());
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        services.AddSingleton<EmailService>();
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var service = new EmailBackgroundService(channel, scopeFactory, badOptions, loggerMock.Object);

        await channel.Writer.WriteAsync(new EmailMessage("fail@test.com", "Fail", "<p>Fail</p>"));
        channel.Writer.TryComplete();

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        await service.StopAsync(cts.Token);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v!.ToString()!.Contains("FAILED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesSingleMessage()
    {
        var channel = Channel.CreateUnbounded<EmailMessage>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        var services = new ServiceCollection();
        services.AddSingleton(_options);
        services.AddSingleton(new EmailRenderer());
        services.AddSingleton(Mock.Of<ILogger<EmailService>>());
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        services.AddSingleton<EmailService>();
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var service = new EmailBackgroundService(channel, scopeFactory, _options,
            Mock.Of<ILogger<EmailBackgroundService>>());

        await channel.Writer.WriteAsync(new EmailMessage("test@test.com", "Subject", "<p>Body</p>"));

        await service.StartAsync(default);
        await Task.Delay(50);
        await service.StopAsync(default);

        var remaining = 0;
        while (channel.Reader.TryRead(out _)) remaining++;
        remaining.Should().BeLessThan(2);
    }
}