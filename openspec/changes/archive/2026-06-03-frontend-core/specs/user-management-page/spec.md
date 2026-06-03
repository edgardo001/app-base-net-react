## ADDED Requirements

### Requirement: Users management page
The system SHALL have a users page with CRUD operations in a table layout.

#### Scenario: User list with pagination
- **WHEN** the users page loads
- **THEN** it SHALL fetch paginated users with search and page params
- **THEN** the table SHALL show columns: Email, Nombre, Estado, Confirmado, Último Login, Creado, Acciones

#### Scenario: Search users
- **WHEN** the user types in the search input
- **THEN** the search param SHALL update and results SHALL refresh

#### Scenario: Create user modal
- **WHEN** "Nuevo" button is clicked
- **THEN** a modal SHALL open with fields: email, firstName, lastName, password, role selection
- **THEN** form validation SHALL use react-hook-form + Zod
- **THEN** on submit, SHALL POST /api/users and refresh the list

#### Scenario: Edit user modal
- **WHEN** edit action is clicked on a user row
- **THEN** a modal SHALL open with pre-populated fields and current roles selected

#### Scenario: Toggle user active
- **WHEN** activate/deactivate action is clicked
- **THEN** SHALL PATCH /api/users/{id}/activate

#### Scenario: Soft delete user
- **WHEN** delete action is clicked
- **THEN** a confirm dialog SHALL appear
- **THEN** on confirm, SHALL DELETE /api/users/{id}
