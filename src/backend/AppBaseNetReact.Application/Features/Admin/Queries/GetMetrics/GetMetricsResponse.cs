namespace AppBaseNetReact.Application.Features.Admin.Queries.GetMetrics;

public class GetMetricsResponse
{
    public double UptimeSeconds { get; set; }
    public long MemoryBytes { get; set; }
    public long TotalRequests { get; set; }
    public int GcCollectionsGen0 { get; set; }
    public int GcCollectionsGen1 { get; set; }
    public int GcCollectionsGen2 { get; set; }
    public int ThreadPoolThreads { get; set; }
    public DateTime Timestamp { get; set; }
}
