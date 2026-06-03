## ADDED Requirements

### Requirement: Zustand auth store
The system SHALL have a Zustand store managing authentication state.

#### Scenario: Login action
- **WHEN** login(email, password) is called with valid credentials
- **THEN** the store SHALL save accessToken and refreshToken to localStorage
- **THEN** the store SHALL set user, permissions, isAuthenticated=true
- **THEN** if passwordExpired is true, the store SHALL return true

#### Scenario: Logout action
- **WHEN** logout() is called
- **THEN** the store SHALL POST /auth/logout with refreshToken
- **THEN** the store SHALL clear localStorage and reset state

#### Scenario: Check auth on app load
- **WHEN** checkAuth() is called and a valid accessToken exists in localStorage
- **THEN** the store SHALL GET /profile to verify the token
- **THEN** the store SHALL set user and isAuthenticated=true on success
- **THEN** the store SHALL set isAuthenticated=false on failure

#### Scenario: Authentication initialization
- **WHEN** the store is created
- **THEN** isAuthenticated SHALL be initialized from localStorage (synchronous, no flash)
