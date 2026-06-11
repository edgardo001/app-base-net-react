using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Infrastructure.Email;

public class EmailBackgroundService : BackgroundService
{
    private readonly Channel<EmailMessage> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailBackgroundService> _logger;
    private readonly EmailOptions _options;

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(25),
        TimeSpan.FromSeconds(125)
    ];

    public EmailBackgroundService(
        Channel<EmailMessage> channel,
        IServiceScopeFactory scopeFactory,
        IOptions<EmailOptions> options,
        ILogger<EmailBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailBackgroundService started");

        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessWithRetryAsync(message, stoppingToken);
        }

        _logger.LogInformation("EmailBackgroundService stopped");
    }

    private async Task ProcessWithRetryAsync(EmailMessage message, CancellationToken ct)
    {
        for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                await emailService.SendNowAsync(message.To, message.Subject, message.HtmlBody, ct);

                _logger.LogInformation(
                    "Background email sent to {To}: {Subject} (attempt {Attempt})",
                    message.To, message.Subject, attempt + 1);

                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < RetryDelays.Length)
            {
                _logger.LogWarning(
                    ex,
                    "Background email FAILED for {To} (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    message.To, attempt + 1, RetryDelays.Length + 1, RetryDelays[attempt].TotalSeconds);

                await Task.Delay(RetryDelays[attempt], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background email PERMANENTLY FAILED for {To}: {Subject}. All {Max} attempts exhausted.",
                    message.To, message.Subject, RetryDelays.Length + 1);
            }
        }
    }
}
