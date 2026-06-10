using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Admin.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, GetDashboardResponse>
{
    private readonly IUnitOfWork _uow;

    public GetDashboardQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetDashboardResponse> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var totalUsers = await _uow.Users.CountAsync(null, ct);
        var activeUsers = await _uow.Users.CountAsync(u => u.IsActive, ct);
        var inactiveUsers = await _uow.Users.CountAsync(u => !u.IsActive, ct);
        var newUsersLast7Days = await _uow.Users.CountAsync(
            u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7), ct);

        var expiredPasswords = await _uow.Users.CountAsync(
            u => u.IsActive && u.LastPasswordChangeAt == null, ct);
        var expiringSoonPasswords = await _uow.Users.CountAsync(
            u => u.IsActive
                && u.LastPasswordChangeAt != null
                && u.LastPasswordChangeAt.Value.AddDays(u.PasswordExpirationDays) <= DateTime.UtcNow.AddDays(7)
                && u.LastPasswordChangeAt.Value.AddDays(u.PasswordExpirationDays) > DateTime.UtcNow, ct);

        return new GetDashboardResponse
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = inactiveUsers,
            NewUsersLast7Days = newUsersLast7Days,
            ExpiredPasswords = expiredPasswords,
            ExpiringSoonPasswords = expiringSoonPasswords
        };
    }
}
