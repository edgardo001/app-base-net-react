## Why

Construir la interfaz de usuario completa del sistema de gestión de usuarios con React 19, TypeScript, Vite, Tailwind CSS v4 y shadcn/ui. El planInicial.ia.md especifica: layout con sidebar colapsable, navbar, auth store con Zustand, API client con axios e interceptor de refresh automático, páginas de login, dashboard, CRUD de usuarios, roles, perfil, admin, cambio de contraseña, y modal de sesión próxima a expirar.

## What Changes

- Aplicación React 19 + Vite + TypeScript + Tailwind CSS v4 + shadcn/ui (@base-ui/react)
- Layout principal: sidebar colapsable (persistencia localStorage) + header con avatar + main content
- Auth store (Zustand): login, logout, checkAuth, user, permissions, isAuthenticated, passwordExpired
- API client (axios): request interceptor (Bearer token), response interceptor (401 → refresh queue → retry)
- ProtectedRoute guard: verifica autenticación en cada navegación
- Páginas: Login, Dashboard (métricas + auditoría), Users (CRUD con modal + paginación + búsqueda), Roles (CRUD con asignación de permisos), Profile (info + cambio de clave + actividad), Admin (audit log + revocación global), ChangePassword (forzado en primer login)
- Componentes UI shadcn: Button, Input, Label, Card, Badge, Avatar, Table, Separator
- SessionWarning modal con countdown de 30s antes de expirar el token
- Dark mode: variables CSS preparadas para modo claro/oscuro (sin toggle aún)
- Vite config: proxy /api a localhost:5011, @ alias para imports

## Capabilities

### New Capabilities

- `react-app-setup`: Proyecto Vite + React 19 + TypeScript + Tailwind CSS v4 con shadcn/ui
- `auth-store-frontend`: Zustand store con login, logout, checkAuth, manejo de tokens en localStorage
- `api-client`: Axios instance con interceptors (Bearer token injection, 401 auto-refresh con cola)
- `app-layout`: Layout principal con sidebar colapsable, header con avatar, main content area
- `login-page`: Página de login con validación de formulario y redirección post-login
- `user-management-page`: CRUD de usuarios con tabla paginada, búsqueda, modal de creación/edición, toggle active, soft delete
- `role-management-page`: CRUD de roles con cards, permisos por módulo, protección de roles de sistema
- `profile-page`: Perfil de usuario con edición de nombre, cambio de contraseña, actividad reciente
- `admin-audit-page`: Página de administración con audit log paginado y revocación global de tokens
- `session-warning`: Modal con countdown de 30 segundos antes de expiración de sesión
- `protected-route`: Guard de ruta que verifica autenticación antes de renderizar
- `ui-component-library`: Componentes shadcn/ui: Button, Input, Label, Card, Badge, Avatar, Table, Separator

### Modified Capabilities

Ninguna.

## Impact

- Nuevos archivos: ~20+ archivos entre componentes, páginas, stores, lib
- Dependencias: react 19, react-router-dom 7, zustand 5, axios, @base-ui/react, lucide-react, react-hook-form, zod, class-variance-authority, clsx, tailwindcss 4
- Proxy Vite: /api → localhost:5011
- UI preparada para dark mode (CSS variables), sin toggle aún
