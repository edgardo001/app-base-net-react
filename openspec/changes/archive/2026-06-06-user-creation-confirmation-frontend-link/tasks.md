## 1. Regla de Oro — Verify baseline

- [x] 1.1 Run `dotnet test app-base-net-react.slnx --nologo` and confirm the previous fix's tests pass. Target: 102/102 before this change.

## 2. Backend — Configuration

- [x] 2.1 Add `FrontendBaseUrl` field to `EmailOptions` with default `"http://localhost:5173"`.
- [x] 2.2 Update `appsettings.json` to set `Email:FrontendBaseUrl = "http://localhost:5173"` (dev default).
- [x] 2.3 Update `appsettings.Production.json` to set `Email:FrontendBaseUrl = ""` (forces explicit config in prod).
- [x] 2.4 Update `appsettings.example.json` to set `Email:FrontendBaseUrl = "https://app.example.com"` (template).

## 3. Backend — Controller

- [x] 3.1 In `UsersController.CreateUser`, replace the line that builds `confirmationLink` from `Request.Scheme://Request.Host` to use `_emailOptions.FrontendBaseUrl.TrimEnd('/') + "/confirm-email?token={token}"`.
- [x] 3.2 Build the solution to confirm 0 errors. `dotnet build app-base-net-react.slnx --nologo`.

## 4. Backend — Tests

- [x] 4.1 Tighten `CreateUser_SendsEmailWithConfirmationLink` to assert the body contains the full `http://localhost:5173/confirm-email?token=` (not just `/confirm-email?token=`).
- [x] 4.2 Add `CreateUser_UsesConfiguredFrontendBaseUrl` that creates a controller with `EmailOptions.FrontendBaseUrl = "https://app.example.com"` and a stub `HttpContext` host of `api.example.com`. Assert the body contains the configured URL and does NOT contain `api.example.com` or `localhost:5011`.
- [x] 4.3 Run `dotnet test app-base-net-react.slnx --nologo` and confirm all tests pass. Target: 103/103 (66 + 37).

## 5. Frontend — Page + Route

- [x] 5.1 Create `src/frontend/src/pages/confirm-email.tsx` mirroring the `ResetPasswordPage` layout (Card / shadcn). The page reads `?token=` from `useSearchParams`, POSTs `{ token }` to `/api/auth/confirm-email` on mount (via `useEffect` with a `cancelled` flag), and shows 3 states: pending (spinner + "Confirmando tu correo…"), success ("Correo confirmado" + `<Link to="/login">`), error (API message + `<Link to="/login">`). Uses `buttonVariants` to style the links (the `Button` shadcn component does not support `asChild` in this project — uses `@base-ui/react/button`).
- [x] 5.2 Register the page in `src/frontend/src/App.tsx` as a public route at `/confirm-email` (outside `<ProtectedRoute>`, like `/forgot-password` and `/reset-password`).
- [x] 5.3 Run `cd src/frontend && npm run build` and confirm `built in` with no TypeScript errors.

## 6. OpenSpec documentation

- [x] 6.1 Create `openspec/changes/user-creation-confirmation-frontend-link/` with `.openspec.yaml`, `proposal.md`, `design.md`, `specs/user-creation/spec.md`, `tasks.md`.
- [x] 6.2 Add 2 new requirements under the existing `user-creation` capability: "Confirmation Link Points To Configured Frontend URL" (3 scenarios) and "Frontend Provides A Public Confirm-Email Page" (4 scenarios).
- [x] 6.3 Run `openspec validate user-creation-confirmation-frontend-link --strict` and confirm `"Change is valid"`.
- [x] 6.4 Run `openspec archive user-creation-confirmation-frontend-link -y` to sync 2 requirements to `openspec/specs/user-creation/spec.md`. Expect `+ 2 added`.

## 7. Final validation

- [x] 7.1 `dotnet build app-base-net-react.slnx --nologo` → 0 errors.
- [x] 7.2 `dotnet test app-base-net-react.slnx --nologo` → 103/103 pass.
- [x] 7.3 `cd src/frontend && npm run build` → `built in` clean.
- [x] 7.4 Commit atomically in 2 commits: one for backend (`fix(users): use configured frontend URL for confirmation link`), one for frontend (`feat(frontend): add /confirm-email page`).
