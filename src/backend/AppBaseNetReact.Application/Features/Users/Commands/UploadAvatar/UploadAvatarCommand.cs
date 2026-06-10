using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.UploadAvatar;

public sealed record UploadAvatarCommand(
    Guid UserId,
    Stream FileStream,
    string FileName,
    string IpAddress,
    string UserAgent) : IRequest<UploadAvatarOutcome>;
