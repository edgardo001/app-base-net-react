using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Infrastructure.Email;

public class EmailQueueItem
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
}

public class EmailQueueService
{
    private readonly ConcurrentQueue<EmailQueueItem> _queue = new();

    public void Enqueue(string to, string subject, string htmlBody)
    {
        _queue.Enqueue(new EmailQueueItem { To = to, Subject = subject, HtmlBody = htmlBody });
    }

    public bool TryDequeue(out EmailQueueItem? item)
    {
        return _queue.TryDequeue(out item);
    }

    public int Count => _queue.Count;
}

public class EmailJob : IEmailJob
{
    private readonly EmailQueueService _queue;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailJob> _logger;

    public EmailJob(EmailQueueService queue, IEmailService emailService, ILogger<EmailJob> logger)
    {
        _queue = queue;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessQueueAsync(CancellationToken ct)
    {
        var processed = 0;
        while (_queue.TryDequeue(out var item) && !ct.IsCancellationRequested)
        {
            try
            {
                await _emailService.SendEmailAsync(item!.To, item.Subject, item.HtmlBody, ct);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued email to {To}", item!.To);
            }
        }

        if (processed > 0)
            _logger.LogInformation("Processed {Count} queued emails", processed);
    }
}

public interface IEmailJob
{
    Task ProcessQueueAsync(CancellationToken ct);
}
