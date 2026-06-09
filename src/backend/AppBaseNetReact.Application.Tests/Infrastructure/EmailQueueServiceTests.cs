using FluentAssertions;
using AppBaseNetReact.Infrastructure.Email;

namespace AppBaseNetReact.Application.Tests.Infrastructure;

public class EmailQueueServiceTests
{
    private readonly EmailQueueService _queue = new();

    [Fact]
    public void Enqueue_IncrementsCount()
    {
        _queue.Enqueue("test@test.com", "Subject", "<p>Body</p>");

        _queue.Count.Should().Be(1);
    }

    [Fact]
    public void TryDequeue_WhenEmpty_ReturnsFalse()
    {
        var result = _queue.TryDequeue(out var item);

        result.Should().BeFalse();
        item.Should().BeNull();
    }

    [Fact]
    public void TryDequeue_WhenNotEmpty_ReturnsItem()
    {
        _queue.Enqueue("test@test.com", "Subject", "<p>Body</p>");

        var result = _queue.TryDequeue(out var item);

        result.Should().BeTrue();
        item.Should().NotBeNull();
        item!.To.Should().Be("test@test.com");
        item.Subject.Should().Be("Subject");
        item.HtmlBody.Should().Be("<p>Body</p>");
    }

    [Fact]
    public void TryDequeue_DequeuesInFIFOOrder()
    {
        _queue.Enqueue("first@test.com", "First", "body1");
        _queue.Enqueue("second@test.com", "Second", "body2");

        _queue.TryDequeue(out var first);
        _queue.TryDequeue(out var second);

        first!.To.Should().Be("first@test.com");
        second!.To.Should().Be("second@test.com");
    }

    [Fact]
    public void Enqueue_MultipleItems_AllCanBeDequeued()
    {
        for (var i = 0; i < 5; i++)
            _queue.Enqueue($"user{i}@test.com", $"Subject {i}", $"body {i}");

        var count = 0;
        while (_queue.TryDequeue(out _)) count++;

        count.Should().Be(5);
    }
}
