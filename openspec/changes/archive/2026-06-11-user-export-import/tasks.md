## 1. Baseline — Regla de Oro

- [x] 1.1 Run `dotnet test` — confirm 247/247 green (400/400 with prior changes)

## 2. Application — Export infrastructure

- [x] 2.1 Add `CsvHelper` NuGet package to `Application.csproj`
- [x] 2.2 Create `Application/Features/Users/Queries/ExportUsers/ExportUsersQuery.cs` — record with same filters as `GetUsersQuery`
- [x] 2.3 Create `ExportUsersQueryHandler.cs` — returns CSV as byte array using CsvHelper
- [x] 2.4 Create `ExportUsersQueryValidator.cs`
- [x] 2.5 Run `dotnet build` — confirm 0 errors

## 3. WebApi — Export endpoint

- [x] 3.1 Add `GET /api/users/export` to `UsersController` — returns `FileContentResult` with `text/csv`
- [x] 3.2 Protected by class-level `[Authorize]` (HasPermission attribute no existe)
- [x] 3.3 Add tests: export all (returns CSV), export filtered (passes search/sort), unauthorized → 403 (class-level)

## 4. Frontend — Export button

- [x] 4.1 Add "Exportar" button to the users page grid toolbar
- [x] 4.2 Wire to `GET /api/users/export` with current filters as query params
- [x] 4.3 Download triggers browser file download via `window.open`
- [x] 4.4 Run `npm run build` — confirm clean build

## 5. Application — Import infrastructure

- [x] 5.1 Create `ImportUsersCommand.cs` — record with `Stream FileContent` + `string FileName`
- [x] 5.2 Create `ImportUsersCommandHandler.cs` — parse CSV with CsvHelper, validate each row, create users, return `ImportUsersResult`
- [x] 5.3 Create `ImportUsersCommandValidator.cs` — validate file extension
- [x] 5.4 Create `ImportUsersResult.cs` + `ImportErrorRow` record
- [x] 5.5 Run `dotnet build` — confirm 0 errors

## 6. WebApi — Import endpoint

- [x] 6.1 Add `POST /api/users/import` to `UsersController` — accepts `IFormFile`, delegates to MediatR
- [x] 6.2 Protected by class-level `[Authorize]`
- [x] 6.3 Add tests: valid CSV → Ok, no file → 400, bad extension → 400, empty file → 400

## 7. Frontend — Import modal

- [x] 7.1 Add "Importar" button to the users page grid toolbar opening a modal
- [x] 7.2 Modal with drag & drop zone for CSV file
- [x] 7.3 On upload, show progress spinner, then result report (created count + errors)
- [x] 7.4 Run `npm run build` — confirm clean build

## 8. Final validation

- [x] 8.1 Run `dotnet build` — 0 errors
- [x] 8.2 Run `dotnet test` — 400/400 pass (256 Application + 144 WebApi)
- [x] 8.3 Run `npm run build` — clean build
