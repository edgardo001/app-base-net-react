## Context

The React 19 frontend uses shadcn/ui v4, Tailwind CSS v4, and Zustand for state. The layout, auth flows, and basic CRUD pages are functional. Key gaps are UX polish (theme, avatar, table interactions) and a missing permissions page.

## Goals / Non-Goals

**Goals:**
- Avatar upload with drag-and-drop and webcam capture (planInicial §8.4)
- Full permissions page (planInicial §8.2)
- Dark/light theme toggle (planInicial §8.1)
- Server-side sorting and filtering in users table (planInicial §8.2)
- Users-by-role view (planInicial §8.2)
- Dashboard improvements (planInicial §8.2)

**Non-Goals:**
- Backend changes (covered in `fase3-backend-completion`)
- Export/Import CSV (post-MVP)
- Session management UI (post-MVP)
- Internationalization (post-MVP)

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Avatar upload** | Single `AvatarUpload` component with tabs (Upload / Webcam) | Reusable in `profile.tsx` and `users.tsx` modal. Tab UI is intuitive. |
| **Webcam** | `navigator.mediaDevices.getUserMedia` + canvas capture | Standard browser API. Fallback to file upload if unavailable. |
| **Theme toggle** | `useTheme` hook + `localStorage` + `<html class="dark">` | Follows shadcn/ui pattern. CSS variables already defined in `index.css`. |
| **Theme persistence** | Inline `<script>` in `index.html` reads localStorage before render | Prevents flash of incorrect theme (FOUC). |
| **Sorting UI** | Click column header → toggle asc/desc/none. Visual arrow indicator. | Standard table UX. Backend already supports `sortBy`/`sortDesc`. |
| **Filtering UI** | Dropdown filters above table for status, role, date range | Simple selects, no complex filter builder needed for MVP. |
| **Permissions page** | Table grouped by module with badge counts | Matches existing `GET /permissions/modules` endpoint shape. |
| **Users by role** | Modal from roles page showing user list | Lighter than a full page navigation. |

## Risks / Trade-offs

- **[getUserMedia]** Only works in secure contexts (HTTPS/localhost). Fallback to file upload is essential.
- **[Theme flash]** Inline script in index.html is the only reliable way to prevent FOUC. Must execute before React mounts.
- **[Table performance]** Server-side sorting/filtering is already supported by backend. No client-side concerns.
