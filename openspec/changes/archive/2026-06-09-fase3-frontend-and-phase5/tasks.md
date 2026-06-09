## 1. Theme Toggle (Dark/Light)

- [x] 1.1 Add inline `<script>` in `index.html` that reads `localStorage('theme')` and sets `class="dark"` on `<html>` before React mounts (prevents FOUC)
- [x] 1.2 Create `src/frontend/src/hooks/use-theme.ts` hook — reads/writes theme to localStorage, applies `dark` class to `<html>`, respects `prefers-color-scheme`
- [x] 1.3 Create `src/frontend/src/components/ui/theme-toggle.tsx` — button with Sun/Moon icons, calls `useTheme` to toggle
- [x] 1.4 Integrate `ThemeToggle` in `header.tsx` next to the user avatar/name

## 2. Avatar Upload Frontend

- [x] 2.1 Create `src/frontend/src/components/ui/avatar-upload.tsx` — component with tabs (Upload / Webcam), drag-and-drop zone, file picker, preview, validation (extension, size)
- [x] 2.2 Create `src/frontend/src/components/ui/webcam-capture.tsx` — component using `getUserMedia`, canvas capture, retake button, fallback message if unavailable
- [x] 2.3 Integrate `AvatarUpload` in `profile.tsx` — replace static avatar display with clickable avatar that opens upload modal
- [x] 2.4 Add avatar preview in `users.tsx` edit modal — show current avatar, allow upload

## 3. Permissions Page

- [x] 3.1 Implement `permissions.tsx` — fetch `GET /api/permissions/modules`, display as grouped table/cards by module, show code/name/description for each permission
- [x] 3.2 Add "No permissions found" empty state

## 4. Advanced User Table

- [x] 4.1 Add sorting state (`sortBy`, `sortDesc`) to `users.tsx` — send params in API request
- [x] 4.2 Add clickable column headers with sort indicator arrows (↑↓) in users table
- [x] 4.3 Add filter dropdowns above users table — status (Active/Inactive), role (fetch from API), clear button
- [x] 4.4 Send filter params (`isActive`, `roleId`) in API request

## 5. Users by Role View

- [x] 5.1 Add "View Users" button in each role card in `roles.tsx`
- [x] 5.2 Create modal or expandable section that fetches `GET /api/roles/{id}/users` and displays user list
- [x] 5.3 Handle empty state ("No users assigned")

## 6. Dashboard Improvements

- [x] 6.1 Add "Accounts nearing password expiry" card in `dashboard.tsx` — call new backend metric endpoint or derive from existing data
- [x] 6.2 Improve error handling in `login.tsx` — detect network errors vs credential errors, show differentiated messages

## 7. Final Validation

- [x] 7.1 `cd src/frontend && npm run build` — no errors
- [x] 7.2 Manual smoke test: theme toggle, avatar upload, permissions page, users sorting/filtering
