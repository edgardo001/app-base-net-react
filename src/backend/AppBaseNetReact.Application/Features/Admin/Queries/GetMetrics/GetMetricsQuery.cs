using MediatR;

namespace AppBaseNetReact.Application.Features.Admin.Queries.GetMetrics;

public record GetMetricsQuery : IRequest<GetMetricsResponse>;
