using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace AppBaseNetReact.Application.Features.Profile.Commands.UploadAvatar;

public sealed class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, UploadAvatarOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;
    private readonly StorageOptions _storageOptions;

    public UploadAvatarCommandHandler(
        IUnitOfWork uow,
        IFileStorageService storage,
        IOptions<StorageOptions> storageOptions)
    {
        _uow = uow;
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    public async Task<UploadAvatarOutcome> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return UploadAvatarOutcome.UserNotFound();

        if (!_storageOptions.AllowedExtensions.Contains(request.Extension))
            return UploadAvatarOutcome.InvalidExtension();

        if (request.FileStream.Length > _storageOptions.MaxFileSize)
            return UploadAvatarOutcome.FileTooLarge();

        var fileName = await _storage.SaveFileAsync(request.FileStream, request.Extension, ct);
        user.SetAvatar(fileName);
        await _uow.SaveChangesAsync(ct);

        return UploadAvatarOutcome.Success(fileName);
    }
}
