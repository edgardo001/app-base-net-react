## ADDED Requirements

### Requirement: Profile page
The system SHALL have a profile page for the authenticated user.

#### Scenario: View profile information
- **WHEN** the profile page loads
- **THEN** it SHALL display user's avatar (initials fallback), name, and email
- **THEN** it SHALL show a form to edit firstName and lastName
- **THEN** it SHALL show recent activity log (last 20 entries)

#### Scenario: Update profile
- **WHEN** the profile form is submitted
- **THEN** SHALL PUT /api/profile with firstName and lastName
- **THEN** the auth store state SHALL be updated
- **THEN** a success message SHALL be displayed

#### Scenario: Change password
- **WHEN** the password change form is submitted with currentPassword, newPassword, confirmPassword
- **THEN** SHALL POST /api/auth/change-password
- **THEN** on success, SHALL display confirmation message
- **THEN** on error, SHALL display error message from API
