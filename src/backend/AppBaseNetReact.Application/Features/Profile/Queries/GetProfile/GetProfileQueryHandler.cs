using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, GetProfileResponse?>
{
    private readonly IUnitOfWork _uow;

    public GetProfileQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetProfileResponse?> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null) return null;

        return new GetProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarPath = user.AvatarPath
        };
    }
}
