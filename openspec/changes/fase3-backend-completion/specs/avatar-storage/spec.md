## ADDED Requirements

### Requirement: Upload user avatar (admin)
The system SHALL allow an admin to upload an avatar image for any user via `POST /api/users/{id}/avatar`.

#### Scenario: Successful avatar upload
- **WHEN** `POST /api/users/{id}/avatar` is called with a valid image file (JPEG, PNG, or WebP, max 5 MB)
- **THEN** the system SHALL store the file with a random filename under `Storage.BasePath`, update `User.AvatarPath`, and return `200 OK` with the avatar filename

#### Scenario: File too large
- **WHEN** `POST /api/users/{id}/avatar` is called with a file exceeding 5 MB
- **THEN** the system SHALL return `400 Bad Request` with message "File size exceeds maximum allowed (5 MB)"

#### Scenario: Invalid file type
- **WHEN** `POST /api/users/{id}/avatar` is called with a file that is not JPEG, PNG, or WebP
- **THEN** the system SHALL return `400 Bad Request` with message "File type not allowed. Allowed types: .jpg, .jpeg, .png, .webp"

### Requirement: Upload own avatar (profile)
The system SHALL allow a user to upload their own avatar via `PUT /api/profile/avatar`.

#### Scenario: Successful own avatar upload
- **WHEN** `PUT /api/profile/avatar` is called with a valid image file
- **THEN** the system SHALL store the file, update the authenticated user's `AvatarPath`, and return `200 OK`

### Requirement: Get user avatar
The system SHALL serve the user's avatar image via `GET /api/users/{id}/avatar`.

#### Scenario: Avatar exists
- **WHEN** `GET /api/users/{id}/avatar` is called and the user has an avatar
- **THEN** the system SHALL return the image file with the appropriate `Content-Type`

#### Scenario: Avatar does not exist
- **WHEN** `GET /api/users/{id}/avatar` is called and the user has no avatar
- **THEN** the system SHALL return `404 Not Found`

### Requirement: File storage configuration
The system SHALL store avatar files in a configurable directory specified by `Storage:BasePath` in configuration.

#### Scenario: Default storage path
- **WHEN** no `Storage:BasePath` is configured
- **THEN** the system SHALL use `/app/storage/avatars` as the default path
