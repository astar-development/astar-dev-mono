## ADDED Requirements

### Requirement: Three themes are supported
The system SHALL support exactly three themes: `light`, `dark`, and `colourful`. All themes MUST meet WCAG AA contrast requirements.

#### Scenario: Light theme renders with correct palette
- **WHEN** the active theme is `light`
- **THEN** the page uses white/near-white backgrounds with dark text and soft pastel accents

#### Scenario: Dark theme renders with correct palette
- **WHEN** the active theme is `dark`
- **THEN** the page uses deep navy/slate backgrounds with light text and muted accents

#### Scenario: Colourful theme renders with correct palette
- **WHEN** the active theme is `colourful`
- **THEN** the page uses vibrant, hue-shifted accent colours with high-energy styling while maintaining WCAG AA text contrast

### Requirement: Theme is applied via data-theme attribute on html element
The system SHALL apply the active theme by setting `data-theme="<theme>"` on the `<html>` element. All theme-specific styles SHALL be defined as CSS custom properties scoped to `[data-theme]` selectors.

#### Scenario: data-theme attribute reflects active theme
- **WHEN** the active theme is `colourful`
- **THEN** the `<html>` element has `data-theme="colourful"`

### Requirement: Theme persists across sessions
The system SHALL persist the selected theme to `localStorage` under the key `fab4kids-theme`. On subsequent visits, the stored value SHALL be applied before first paint.

#### Scenario: Selected theme restored on return visit
- **WHEN** a visitor selects `dark` theme and returns to the site in a new tab
- **THEN** the dark theme is applied immediately without flash of another theme

### Requirement: Default theme respects prefers-color-scheme
When no theme is stored in `localStorage`, the system SHALL resolve the default theme: `dark` if `prefers-color-scheme: dark`, otherwise `light`. The `colourful` theme is never auto-applied.

#### Scenario: Dark OS preference results in dark default
- **WHEN** no theme is stored and the OS preference is dark
- **THEN** the `dark` theme is applied on first visit

#### Scenario: Light OS preference results in light default
- **WHEN** no theme is stored and the OS preference is light or unset
- **THEN** the `light` theme is applied on first visit

### Requirement: Theme switcher is accessible from all pages
The system SHALL include a theme switcher control in the site header. The control SHALL allow switching between all three themes and SHALL visually indicate the currently active theme.

#### Scenario: Theme switcher changes active theme immediately
- **WHEN** a visitor clicks the `colourful` option in the theme switcher
- **THEN** the page theme changes immediately without page reload and the selection is persisted to `localStorage`

### Requirement: No flash of unstyled theme on load
The system SHALL include an inline `<script>` in `<head>` that reads `localStorage` and sets `data-theme` before any CSS is rendered, preventing a flash of the wrong theme.

#### Scenario: Correct theme applied before first paint
- **WHEN** a visitor with a stored `dark` preference loads any page
- **THEN** no flash of `light` theme occurs before `dark` is applied
