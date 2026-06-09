## ADDED Requirements

### Requirement: Theme toggle button
The system SHALL provide a theme toggle button in the header to switch between light and dark modes.

#### Scenario: Toggle to dark mode
- **WHEN** user clicks the theme toggle button while in light mode
- **THEN** the system SHALL add `class="dark"` to `<html>`, persist preference to localStorage, and all components switch to dark theme

#### Scenario: Toggle to light mode
- **WHEN** user clicks the theme toggle button while in dark mode
- **THEN** the system SHALL remove `class="dark"` from `<html>` and persist preference

#### Scenario: Theme persists across sessions
- **WHEN** user sets theme to dark and reloads the page
- **THEN** the system SHALL load dark theme from localStorage before render (no flash of light theme)

#### Scenario: System preference detection
- **WHEN** user has no saved theme preference
- **THEN** the system SHALL use `prefers-color-scheme: dark` media query to determine initial theme
