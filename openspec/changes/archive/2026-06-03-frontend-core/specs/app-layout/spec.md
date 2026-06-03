## ADDED Requirements

### Requirement: Responsive layout with sidebar
The application SHALL have a main layout with collapsible sidebar, header, and content area.

#### Scenario: Sidebar collapse toggle
- **WHEN** the toggle button is clicked
- **THEN** the sidebar SHALL collapse from w-64 to w-16 (icons only)
- **THEN** the collapsed state SHALL be persisted in localStorage

#### Scenario: Navigation items
- **WHEN** the sidebar is rendered
- **THEN** it SHALL show navigation items: Dashboard, Usuarios, Roles, Permisos, Perfil, Admin, Tipo A, Tipo B, Tipo C
- **THEN** each item SHALL use NavLink with active state styling

#### Scenario: Header with user info
- **WHEN** the header is rendered
- **THEN** it SHALL display the user's avatar (initials fallback) and name
- **THEN** it SHALL have a logout button
- **THEN** it SHALL have the sidebar toggle button
