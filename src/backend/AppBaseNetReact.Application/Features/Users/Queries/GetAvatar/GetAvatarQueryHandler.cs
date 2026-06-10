using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Users.Queries.GetAvatar;

public sealed class GetAvatarQueryHandler : IRequestHandler<GetAvatarQuery, GetAvatarOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;

    public GetAvatarQueryHandler(IUnitOfWork uow, IFileStorageService storage)
    {
        _uow = uow;
        _storage = storage;
    }

    public async Task<GetAvatarOutcome> Handle(GetAvatarQuery request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new GetAvatarOutcome(GetAvatarResult.UserNotFound());

        if (string.IsNullOrEmpty(user.AvatarPath))
            return new GetAvatarOutcome(GetAvatarResult.NoAvatar());

        var filePath = await _storage.GetFilePathAsync(user.AvatarPath);
        if (filePath == null)
            return new GetAvatarOutcome(GetAvatarResult.FileNotFound());

        var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return new GetAvatarOutcome(GetAvatarResult.Success(filePath, contentType));
    }
}
