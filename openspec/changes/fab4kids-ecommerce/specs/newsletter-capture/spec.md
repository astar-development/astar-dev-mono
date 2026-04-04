## ADDED Requirements

### Requirement: Newsletter signup form is available on the homepage
The system SHALL include a newsletter signup form on the homepage. The form SHALL collect email address only (MVP). Submission SHALL be handled by an Astro API route (`POST /api/newsletter`).

#### Scenario: Valid email is accepted
- **WHEN** a visitor submits a valid email address via the newsletter form
- **THEN** the email is stored and a success confirmation message is displayed

#### Scenario: Invalid email is rejected client-side
- **WHEN** a visitor submits a malformed email address
- **THEN** a validation error is shown and the form is not submitted

### Requirement: Explicit GDPR opt-in is required
The newsletter form SHALL include an unchecked opt-in checkbox. The form SHALL NOT be submittable unless the checkbox is checked. The checkbox label SHALL clearly state that the user agrees to receive marketing emails and link to the privacy policy.

#### Scenario: Form not submittable without opt-in
- **WHEN** a visitor enters a valid email but does not check the opt-in checkbox
- **THEN** the form cannot be submitted and an error message is shown

#### Scenario: Opt-in checkbox is unchecked by default
- **WHEN** the newsletter form is rendered
- **THEN** the opt-in checkbox is unchecked

### Requirement: Captured emails are stored server-side
The API route SHALL store the email address and opt-in timestamp. Storage mechanism is TBD (open question in design.md); the spec defines the data shape only.

#### Scenario: Email and timestamp stored on valid submission
- **WHEN** a visitor submits a valid email with opt-in checked
- **THEN** the email address and ISO 8601 UTC opt-in timestamp are persisted

### Requirement: Duplicate email submissions are handled gracefully
The system SHALL NOT store the same email address twice. A duplicate submission SHALL return a success response (to avoid confirming which emails are registered).

#### Scenario: Duplicate email returns success silently
- **WHEN** a visitor submits an email address already in the store
- **THEN** a success message is displayed and no duplicate record is created
