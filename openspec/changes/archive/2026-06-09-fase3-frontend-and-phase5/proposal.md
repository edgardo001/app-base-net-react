## Why

The frontend is ~75% complete for the full user management experience. Key gaps: avatar upload with webcam support, a real permissions page (currently a placeholder), advanced table features (sorting, filtering), dark/light theme toggle, and improved dashboard metrics. These improvements bring the UI to production quality.

## What Changes

- **ADD** Avatar upload component with drag-and-drop + webcam capture for profile and user management pages
- **ADD** Full Permissions page showing all permissions grouped by module
- **ADD** Advanced table features: server-side sorting (clickable column headers) and filters (status, role, date) in users page
- **ADD** Theme toggle (dark/light) with localStorage persistence and system preference detection
- **ADD** Users-by-role view (button in roles page, modal or section showing assigned users)
- **ADD** Improved dashboard with additional metrics (accounts nearing password expiry)
- **ADD** Better network error handling in login page

## Capabilities

### New Capabilities
- `avatar-upload-ui`: Drag-and-drop file upload + webcam capture component
- `permissions-page`: Full permissions listing page grouped by module
- `theme-toggle`: Dark/light mode toggle with persistence
- `advanced-user-table`: Server-side sorting and filtering in users page
- `users-by-role-view`: View users assigned to a role from the roles page

### Modified Capabilities
- `dashboard`: Additional metrics (password expiry warnings)

## Impact

- **New files**: `avatar-upload.tsx`, `webcam-capture.tsx`, `theme-toggle.tsx`, `use-theme.ts`
- **Modified files**: `profile.tsx`, `users.tsx`, `roles.tsx`, `permissions.tsx`, `dashboard.tsx`, `header.tsx`, `login.tsx`, `index.html`
- **Dependencies**: Browser `getUserMedia` API (webcam), `canvas` API (capture)
- **Docker**: No changes (frontend is static)
