using MediatR;

namespace AppBaseNetReact.Application.Features.Admin.Queries.GetAuditLog;

public sealed record GetAuditLogQuery(int Page, int PageSize) : IRequest<GetAuditLogResponse>;
