using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Admin.Queries.GetAuditLog;

public sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, GetAuditLogResponse>
{
    private readonly IUnitOfWork _uow;

    public GetAuditLogQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetAuditLogResponse> Handle(GetAuditLogQuery request, CancellationToken ct)
    {
        var result = await _uow.AuditLogs.GetPagedAsync(request.Page, request.PageSize, ct: ct);

        return new GetAuditLogResponse
        {
            Items = result.Items.Select(l => new AuditLogItemDto
            {
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Details = l.Details,
                UserId = l.UserId,
                CreatedAt = l.CreatedAt
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };
    }
}
