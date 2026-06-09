## ADDED Requirements

### Requirement: Avatar upload with drag-and-drop
The system SHALL provide a drag-and-drop file upload component for avatar images.

#### Scenario: Successful upload via drag-and-drop
- **WHEN** user drags an image file onto the avatar upload area
- **THEN** the system SHALL show a preview of the image and upload it via `PUT /api/profile/avatar` or `POST /api/users/{id}/avatar`

#### Scenario: Successful upload via file picker
- **WHEN** user clicks the upload area and selects an image file
- **THEN** the system SHALL show a preview and upload the file

#### Scenario: Invalid file type
- **WHEN** user drops or selects a file that is not JPEG, PNG, or WebP
- **THEN** the system SHALL show an error toast "File type not allowed"

#### Scenario: File too large
- **WHEN** user uploads a file exceeding 5 MB
- **THEN** the system SHALL show an error toast "File size exceeds 5 MB"

### Requirement: Webcam capture
The system SHALL provide a webcam capture option for taking a profile photo.

#### Scenario: Webcam available
- **WHEN** user clicks "Take Photo" tab and browser supports getUserMedia
- **THEN** the system SHALL show live webcam preview with a "Capture" button

#### Scenario: Capture photo
- **WHEN** user clicks "Capture" while webcam is active
- **THEN** the system SHALL capture the current frame, show a preview, and upload it as avatar

#### Scenario: Webcam not available
- **WHEN** user clicks "Take Photo" tab but getUserMedia is not available (HTTP or no camera)
- **THEN** the system SHALL show a message "Camera not available" and fallback to file upload tab

#### Scenario: Retake photo
- **WHEN** user captures a photo but wants to retake
- **THEN** the system SHALL show "Retake" button that returns to live preview
