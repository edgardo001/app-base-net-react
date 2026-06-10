using MediatR;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Users.Commands.UploadAvatar;

public sealed class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, UploadAvatarOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;
    private readonly StorageOptions _storageOptions;
    private readonly IMediator _mediator;

    public UploadAvatarCommandHandler(
        IUnitOfWork uow,
        IFileStorageService storage,
        IOptions<StorageOptions> storageOptions,
        IMediator mediator)
    {
        _uow = uow;
        _storage = storage;
        _storageOptions = storageOptions.Value;
        _mediator = mediator;
    }

    public async Task<UploadAvatarOutcome> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new UploadAvatarOutcome(UploadAvatarResult.UserNotFound());

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!_storageOptions.AllowedExtensions.Contains(ext))
            return new UploadAvatarOutcome(UploadAvatarResult.InvalidExtension(string.Join(", ", _storageOptions.AllowedExtensions)));

        if (request.FileStream.Length > _storageOptions.MaxFileSize)
            return new UploadAvatarOutcome(UploadAvatarResult.FileTooLarge(_storageOptions.MaxFileSize));

        var fileName = await _storage.SaveFileAsync(request.FileStream, ext, ct);
        user.SetAvatar(fileName);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new Features.Users.Notifications.AvatarUpdatedNotification(
            user.Id, user.Email, fileName,
            request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"), ct);

        return new UploadAvatarOutcome(UploadAvatarResult.Success(fileName));
    }
}
