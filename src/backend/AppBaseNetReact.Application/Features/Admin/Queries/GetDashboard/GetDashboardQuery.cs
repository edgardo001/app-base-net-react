using MediatR;

namespace AppBaseNetReact.Application.Features.Admin.Queries.GetDashboard;

public sealed record GetDashboardQuery : IRequest<GetDashboardResponse>;
