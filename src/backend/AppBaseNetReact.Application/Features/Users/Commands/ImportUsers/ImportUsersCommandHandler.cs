using System.Globalization;
using System.Security.Cryptography;
using MediatR;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Users.Commands.ImportUsers;

public sealed class ImportUsersCommandHandler : IRequestHandler<ImportUsersCommand, ImportUsersResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasherService _hasher;
    private readonly IRandomPasswordGenerator _passwords;
    private readonly IMediator _mediator;

    public ImportUsersCommandHandler(
        IUnitOfWork uow,
        IPasswordHasherService hasher,
        IRandomPasswordGenerator passwords,
        IMediator mediator)
    {
        _uow = uow;
        _hasher = hasher;
        _passwords = passwords;
        _mediator = mediator;
    }

    public async Task<ImportUsersResult> Handle(ImportUsersCommand request, CancellationToken ct)
    {
        var errors = new List<ImportErrorRow>();
        var created = 0;

        using var reader = new StreamReader(request.FileContent);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        csv.Read();
        csv.ReadHeader();

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            try
            {
                var email = csv.GetField("Email")?.Trim().ToLowerInvariant();
                var firstName = csv.GetField("FirstName")?.Trim();
                var lastName = csv.GetField("LastName")?.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    errors.Add(new ImportErrorRow(rowNumber, "Email is required"));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    errors.Add(new ImportErrorRow(rowNumber, "FirstName is required"));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    errors.Add(new ImportErrorRow(rowNumber, "LastName is required"));
                    continue;
                }

                var existing = await _uow.Users.GetByEmailAsync(email, ct);
                if (existing != null)
                {
                    errors.Add(new ImportErrorRow(rowNumber, $"Duplicate email: {email}"));
                    continue;
                }

                var temporaryPassword = _passwords.Generate();
                var user = User.Create(email, firstName, lastName, _hasher.HashPassword(temporaryPassword));
                user.ForcePasswordChange();

                var confirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                user.SetEmailConfirmationToken(confirmationToken, DateTime.UtcNow.AddHours(24));

                await _uow.Users.AddAsync(user, ct);
                created++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorRow(rowNumber, $"Parse error: {ex.Message}"));
            }
        }

        if (created > 0)
            await _uow.SaveChangesAsync(ct);

        return new ImportUsersResult(created, errors);
    }
}
