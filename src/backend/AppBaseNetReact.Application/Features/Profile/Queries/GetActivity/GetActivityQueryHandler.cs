using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Profile.Queries.GetActivity;

public sealed class GetActivityQueryHandler : IRequestHandler<GetActivityQuery, GetActivityResponse>
{
    private readonly IUnitOfWork _uow;

    public GetActivityQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetActivityResponse> Handle(GetActivityQuery request, CancellationToken ct)
    {
        var logs = await _uow.AuditLogs.GetByUserAsync(request.UserId, 20, ct);

        return new GetActivityResponse
        {
            Items = logs.Select(l => new ActivityItemDto
            {
                Action = l.Action,
                EntityType = l.EntityType,
                Details = l.Details,
                CreatedAt = l.CreatedAt
            }).ToList()
        };
    }
}
