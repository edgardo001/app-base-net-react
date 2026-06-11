using System.Globalization;
using MediatR;
using CsvHelper;
using CsvHelper.Configuration;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Users.Queries.ExportUsers;

public sealed class ExportUsersQueryHandler : IRequestHandler<ExportUsersQuery, byte[]>
{
    private readonly IUnitOfWork _uow;

    public ExportUsersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<byte[]> Handle(ExportUsersQuery request, CancellationToken ct)
    {
        var result = await _uow.Users.GetPagedAsync(
            1, int.MaxValue, null, request.SortBy, request.SortDesc, request.Search, ct);

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteField("Email");
        csv.WriteField("FirstName");
        csv.WriteField("LastName");
        csv.WriteField("IsActive");
        csv.WriteField("EmailConfirmed");
        csv.WriteField("Roles");
        csv.WriteField("CreatedAt");
        csv.NextRecord();

        foreach (var user in result.Items)
        {
            csv.WriteField(user.Email);
            csv.WriteField(user.FirstName);
            csv.WriteField(user.LastName);
            csv.WriteField(user.IsActive.ToString());
            csv.WriteField(user.EmailConfirmed.ToString());
            csv.WriteField(string.Join("; ", user.UserRoles.Select(ur => ur.Role?.Name ?? "")));
            csv.WriteField(user.CreatedAt.ToString("O"));
            csv.NextRecord();
        }

        writer.Flush();
        return ms.ToArray();
    }
}
