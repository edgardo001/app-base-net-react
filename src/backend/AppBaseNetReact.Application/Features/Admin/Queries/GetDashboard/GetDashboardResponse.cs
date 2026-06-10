namespace AppBaseNetReact.Application.Features.Admin.Queries.GetDashboard;

public sealed record GetDashboardResponse
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int InactiveUsers { get; init; }
    public int NewUsersLast7Days { get; init; }
    public int ExpiredPasswords { get; init; }
    public int ExpiringSoonPasswords { get; init; }
}
