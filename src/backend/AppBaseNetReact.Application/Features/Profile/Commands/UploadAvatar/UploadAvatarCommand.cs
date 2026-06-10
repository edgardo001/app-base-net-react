using MediatR;

namespace AppBaseNetReact.Application.Features.Profile.Commands.UploadAvatar;

public sealed record UploadAvatarCommand(
    Guid UserId,
    Stream FileStream,
    string Extension,
    string FileName) : IRequest<UploadAvatarOutcome>;
