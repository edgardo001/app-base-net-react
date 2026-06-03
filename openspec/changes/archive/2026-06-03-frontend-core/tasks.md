## 1. Project Setup

- [x] 1.1 Initialize Vite project with React 19 + TypeScript template
- [x] 1.2 Configure Tailwind CSS v4 with PostCSS
- [x] 1.3 Configure TypeScript strict mode and path aliases (@/ → ./src)
- [x] 1.4 Install and configure @base-ui/react for UI primitives
- [x] 1.5 Install dependencies: zustand, axios, react-router-dom, react-hook-form, zod, @hookform/resolvers, lucide-react, class-variance-authority, clsx

## 2. UI Component Library

- [x] 2.1 Create Button component with variants (default, destructive, outline, secondary, ghost, link) and sizes
- [x] 2.2 Create Input component with focus-ring, aria-invalid, disabled styles
- [x] 2.3 Create Label component with htmlFor support
- [x] 2.4 Create Card component (Card, Header, Title, Description, Action, Content, Footer)
- [x] 2.5 Create Badge component with 7 variants
- [x] 2.6 Create Avatar component (Root, Image, Fallback)
- [x] 2.7 Create Table component (Table, Header, Body, Footer, Row, Head, Cell, Caption)
- [x] 2.8 Create Separator component
- [x] 2.9 Create utility functions (cn with clsx + twMerge)

## 3. Auth Store (Zustand)

- [x] 3.1 Implement useAuthStore with user, permissions, isAuthenticated, passwordExpired state
- [x] 3.2 Implement login action: POST /auth/login, save tokens to localStorage, set state
- [x] 3.3 Implement logout action: POST /auth/logout, clear localStorage, reset state
- [x] 3.4 Implement checkAuth action: GET /profile to verify token on app load
- [x] 3.5 Initialize isAuthenticated synchronously from localStorage

## 4. API Client (Axios)

- [x] 4.1 Create preconfigured Axios instance with baseURL and Content-Type
- [x] 4.2 Implement request interceptor: inject Bearer token from localStorage
- [x] 4.3 Implement response interceptor: detect 401, queue concurrent requests, refresh token
- [x] 4.4 Implement refresh failure handling: clear localStorage, redirect to /login
- [x] 4.5 Implement failed queue: process queued requests after successful refresh

## 5. Layout Components

- [x] 5.1 Create Layout component with sidebar + header + main (Outlet) + SessionWarning
- [x] 5.2 Create Sidebar with 9 NavLink items, collapsible (w-16 / w-64), localStorage persistence
- [x] 5.3 Create Header with sidebar toggle, user avatar/name, logout button
- [x] 5.4 Configure main content area with overflow-y-auto and padding

## 6. Route Protection

- [x] 6.1 Implement ProtectedRoute component with checkAuth on mount
- [x] 6.2 Redirect to /login if not authenticated
- [x] 6.3 Show nothing while checking authentication

## 7. Login Page

- [x] 7.1 Create LoginPage with email + password form
- [x] 7.2 Implement form submission with loading/error states
- [x] 7.3 Redirect to /change-password if password expired, /dashboard otherwise

## 8. Dashboard Page

- [x] 8.1 Create DashboardPage with 4 stat cards (total, active, new 7d, inactive)
- [x] 8.2 Fetch metrics from GET /api/admin/dashboard
- [x] 8.3 Fetch and display recent audit log (last 10 entries)

## 9. Users Page

- [x] 9.1 Create UsersPage with paginated table (page, search, sort)
- [x] 9.2 Implement search input with debounce
- [x] 9.3 Implement create/edit modal with react-hook-form + Zod validation
- [x] 9.4 Implement role selection via clickable badges in modal
- [x] 9.5 Implement toggle active/inactive action
- [x] 9.6 Implement soft delete with confirm dialog
- [x] 9.7 Implement pagination controls (prev/next)

## 10. Roles Page

- [x] 10.1 Create RolesPage with role cards layout
- [x] 10.2 Implement create/edit modal with name, description, permissions
- [x] 10.3 Implement permission toggles grouped by module (via /api/permissions/modules)
- [x] 10.4 Disable delete for system roles
- [x] 10.5 Show system badge on system roles

## 11. Profile Page

- [x] 11.1 Create ProfilePage with avatar display (initials fallback)
- [x] 11.2 Implement profile edit form (firstName, lastName) with react-hook-form
- [x] 11.3 Implement password change form with current/new/confirm
- [x] 11.4 Implement activity log list (last 20 entries)

## 12. Admin Page

- [x] 12.1 Create AdminPage with paginated audit log table
- [x] 12.2 Implement "Revocar Todas las Sesiones" with confirm dialog
- [x] 12.3 Implement refresh button for audit log

## 13. Change Password Page

- [x] 13.1 Create ChangePasswordPage for forced password change on first login
- [x] 13.2 Call login after successful password change
- [x] 13.3 Redirect to /dashboard on success

## 14. Session Warning

- [x] 14.1 Create SessionWarning component with JWT expiration parsing
- [x] 14.2 Implement countdown timer (1 second interval)
- [x] 14.3 Show modal when remaining <= 30 seconds with countdown display
- [x] 14.4 Implement auto-logout when countdown reaches 0

## 15. App Router Configuration

- [x] 15.1 Configure React Router with routes: /login, /change-password, /dashboard, /users, /roles, /permissions, /profile, /admin, /tipo-a, /tipo-b, /tipo-c
- [x] 15.2 Wrap protected routes with ProtectedRoute + Layout

## 16. Vite Configuration

- [x] 16.1 Configure proxy: /api → http://localhost:5011
- [x] 16.2 Configure proxy: /scalar → http://localhost:5011
- [x] 16.3 Configure @ alias → ./src
- [x] 16.4 Add Tailwind CSS v4 plugin
