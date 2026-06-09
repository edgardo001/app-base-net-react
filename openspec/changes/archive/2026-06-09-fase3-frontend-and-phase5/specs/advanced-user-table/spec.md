## ADDED Requirements

### Requirement: Server-side sorting in users table
The system SHALL support server-side sorting by clicking column headers in the users table.

#### Scenario: Sort by column
- **WHEN** user clicks a sortable column header (email, firstName, lastName, createdAt)
- **THEN** the system SHALL send `sortBy=<column>&sortDesc=true/false` to the API and display results in sorted order

#### Scenario: Toggle sort direction
- **WHEN** user clicks an already-sorted column header
- **THEN** the system SHALL toggle between ascending, descending, and no sort

#### Scenario: Sort indicator
- **WHEN** a column is sorted
- **THEN** the system SHALL display an arrow indicator (↑ or ↓) next to the column header

### Requirement: Advanced filters in users table
The system SHALL provide filter controls above the users table.

#### Scenario: Filter by status
- **WHEN** user selects "Active" or "Inactive" from the status filter
- **THEN** the system SHALL send `isActive=true/false` to the API and display only matching users

#### Scenario: Filter by role
- **WHEN** user selects a role from the role filter dropdown
- **THEN** the system SHALL send `roleId=<guid>` to the API and display only users with that role

#### Scenario: Clear filters
- **WHEN** user clicks "Clear filters"
- **THEN** the system SHALL reset all filters and display all users
