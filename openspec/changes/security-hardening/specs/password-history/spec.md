## ADDED Requirements

### Requirement: Password history tracking on change
The system SHALL store a hash of each new password when a user changes their password, keeping only the last N entries (N configurable via `PasswordPolicyService.PasswordHistoryCount`).

#### Scenario: Successful change stores hash
- **WHEN** a user changes their password successfully
- **THEN** the system stores a hash of the new password in the `PasswordHistories` table
- **AND** the system keeps only the last `PasswordHistoryCount` entries for that user (oldest removed)

#### Scenario: Reusing a recent password is rejected
- **WHEN** a user tries to change their password to one that matches any of the last N stored hashes
- **THEN** the system returns a validation error indicating the password was recently used

#### Scenario: PasswordHistoryCount is configurable
- **WHEN** the system starts
- **THEN** it reads `PasswordHistoryCount` from `PasswordPolicyConfig` (default: 5)

### Requirement: Old password auto-cleanup on rotation
The system SHALL automatically remove the oldest password hash when a new one is stored and the count exceeds `PasswordHistoryCount`.

#### Scenario: Oldest hash is removed
- **WHEN** a new password hash is stored and the user has reached `PasswordHistoryCount` entries
- **THEN** the oldest hash for that user is deleted

#### Scenario: No cleanup if below limit
- **WHEN** a new password hash is stored and the user has fewer than `PasswordHistoryCount` entries
- **THEN** no existing entries are removed
