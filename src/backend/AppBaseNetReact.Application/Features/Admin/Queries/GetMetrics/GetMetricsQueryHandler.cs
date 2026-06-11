using System.Diagnostics;
using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Admin.Queries.GetMetrics;

public class GetMetricsQueryHandler : IRequestHandler<GetMetricsQuery, GetMetricsResponse>
{
    private static readonly DateTime _processStart = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    public Task<GetMetricsResponse> Handle(GetMetricsQuery request, CancellationToken ct)
    {
        var process = Process.GetCurrentProcess();
        var response = new GetMetricsResponse
        {
            UptimeSeconds = (DateTime.UtcNow - _processStart).TotalSeconds,
            MemoryBytes = GC.GetTotalMemory(false),
            GcCollectionsGen0 = GC.CollectionCount(0),
            GcCollectionsGen1 = GC.CollectionCount(1),
            GcCollectionsGen2 = GC.CollectionCount(2),
            ThreadPoolThreads = ThreadPool.ThreadCount,
            Timestamp = DateTime.UtcNow
        };
        return Task.FromResult(response);
    }
}
