## ADDED Requirements

### Requirement: Session expiration warning
The system SHALL display a warning modal 30 seconds before JWT expiration.

#### Scenario: Warning modal display
- **WHEN** the remaining token lifetime is <= 30 seconds
- **THEN** a modal SHALL appear with countdown and "Su sesión expirará en X segundos"
- **THEN** two buttons SHALL be shown: "Cerrar Sesión" and "Continuar Sesión"

#### Scenario: Session continuation
- **WHEN** "Continuar Sesión" is clicked
- **THEN** SHALL POST /api/auth/refresh with refreshToken
- **THEN** on success, SHALL save new tokens and close modal

#### Scenario: Auto-logout on expiration
- **WHEN** countdown reaches 0
- **THEN** the modal SHALL close, logout SHALL be called, and SHALL navigate to /login

### Requirement: Route protection
The system SHALL protect authenticated routes from unauthenticated access.

#### Scenario: Protected route
- **WHEN** an unauthenticated user tries to access /dashboard
- **THEN** ProtectedRoute SHALL redirect to /login

#### Scenario: Auth check on mount
- **WHEN** ProtectedRoute mounts
- **THEN** it SHALL call checkAuth() to verify token validity
- **THEN** while checking, SHALL render nothing (loading state)
