## ADDED Requirements

### Requirement: View users by role
The system SHALL allow viewing users assigned to a specific role from the roles page.

#### Scenario: Open users-by-role view
- **WHEN** user clicks "View Users" button on a role card
- **THEN** the system SHALL open a modal or section showing users assigned to that role (fetched from `GET /api/roles/{id}/users`)

#### Scenario: Role with no users
- **WHEN** user opens users-by-role view for a role with no assigned users
- **THEN** the system SHALL display "No users assigned to this role"

#### Scenario: Close users-by-role view
- **WHEN** user clicks "Close" or outside the modal
- **THEN** the system SHALL close the modal/section
