namespace AppBaseNetReact.Application.Features.Users.Commands.ImportUsers;

public sealed record ImportUsersResult(int CreatedCount, List<ImportErrorRow> ErrorRows);

public sealed record ImportErrorRow(int RowNumber, string Message);
