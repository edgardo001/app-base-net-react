## Why

The `EmailConfirmation` email sent by `UsersController.CreateUser` (in the
`user-creation` capability) contained a link of the form
`${Request.Scheme}://${Request.Host}/confirm-email?token={token}`. In
local dev `Request.Host` resolves to `localhost:5011` (the backend
port), so clicking the link landed on a backend route that does not
exist — the user reported a blank page.

This is a real bug with two causes:

1. **The link points to the wrong host.** It uses `Request.Host` (the
   API server), not the frontend server. Even in production this would
   point the user's browser at the JSON API host, where no
   `/confirm-email` HTML page exists.
2. **The frontend has no `/confirm-email` page.** The SPA only ships
   auth-flavoured pages (`/login`, `/forgot-password`,
   `/reset-password`); `confirm-email` was never added. The backend
   confirmation endpoint exists (`POST /api/auth/confirm-email`, from
   `cqrs-auth-confirm-email`) but is unreachable from a normal user
   click.

## What Changes

- **Backend** — Add `EmailOptions.FrontendBaseUrl` (default
  `http://localhost:5173`). `UsersController.CreateUser` now composes
  the confirmation link as
  `{FrontendBaseUrl.TrimEnd('/')}/confirm-email?token={token}` instead
  of using `Request.Scheme`/`Request.Host`. Configurable per
  environment via `Email__FrontendBaseUrl` env var (or
  `Email:FrontendBaseUrl` JSON key). New backend test
  `CreateUser_UsesConfiguredFrontendBaseUrl` proves the host is taken
  from config, not the request.
- **Backend** — `appsettings.json` now sets the dev default
  (`http://localhost:5173`); `appsettings.Production.json` sets it to
  empty to force explicit configuration; `appsettings.example.json`
  documents `https://app.example.com` as the production placeholder.
- **Frontend** — New page `src/frontend/src/pages/confirm-email.tsx`.
  It reads `?token=` from the query string and on mount POSTs
  `{ token }` to `/api/auth/confirm-email`. UI has three states:
  pending (spinner), success (link to `/login`), error (message +
  link to `/login`). Wired as a public route at `/confirm-email` in
  `App.tsx` — outside the `ProtectedRoute` shell because the user is
  not yet logged in at this point.
- **Spec** — Adds 2 requirements to the `user-creation` capability
  (link points to configured frontend URL; frontend has a
  `/confirm-email` page that submits the token and shows the outcome).

## Capabilities

### New Capabilities

- (none)

### Modified Capabilities

- `user-creation` — 2 requirements added.

## Impact

- **Code**:
  - `src/backend/AppBaseNetReact.Infrastructure/Services/EmailOptions.cs`
    (add `FrontendBaseUrl` field with default
    `http://localhost:5173`)
  - `src/backend/AppBaseNetReact.WebApi/Controllers/UsersController.cs`
    (swap `Request.Scheme://Request.Host` for
    `_emailOptions.FrontendBaseUrl.TrimEnd('/')`)
  - `src/backend/AppBaseNetReact.WebApi/appsettings.json`,
    `appsettings.Production.json`, `appsettings.example.json`
    (add `FrontendBaseUrl` key)
  - `src/backend/AppBaseNetReact.WebApi.Tests/Controllers/UsersControllerTests.cs`
    (tighten `CreateUser_SendsEmailWithConfirmationLink` to assert the
    full configured URL; add
    `CreateUser_UsesConfiguredFrontendBaseUrl`)
  - `src/frontend/src/pages/confirm-email.tsx` (new)
  - `src/frontend/src/App.tsx` (add route + import)
- **HTTP contract**: backend `POST /api/users` response shape
  unchanged; the only change is the URL embedded in the email body.
- **Frontend bundle**: +1 small page (~80 lines TSX), no new
  dependencies.
- **Database**: no changes.
- **Dependencies**: no new NuGet or npm packages.
- **Configuration**: `Email:FrontendBaseUrl` (env: `Email__FrontendBaseUrl`).
  Dev default is `http://localhost:5173`. Production must override
  to the real frontend origin.
- **Docker**: no compose changes; the env var can be passed via
  `.env` if running in a container.
