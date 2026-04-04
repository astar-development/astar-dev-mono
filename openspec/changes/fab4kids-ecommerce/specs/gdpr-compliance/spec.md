## ADDED Requirements

### Requirement: Cookie consent banner blocks third-party scripts
The system SHALL display a cookie consent banner on first visit. Third-party scripts (Stripe JS) SHALL NOT be loaded until the visitor accepts cookies. The consent decision SHALL be persisted to `localStorage`.

#### Scenario: Stripe JS not loaded before consent
- **WHEN** a first-time visitor loads any page before accepting cookies
- **THEN** the Stripe JS script is not present in the DOM

#### Scenario: Stripe JS loaded after consent
- **WHEN** a visitor accepts cookies via the consent banner
- **THEN** the Stripe JS script is injected and the banner is dismissed

#### Scenario: Consent decision persisted across sessions
- **WHEN** a visitor has previously accepted cookies
- **THEN** the consent banner is not shown on subsequent visits

#### Scenario: Visitor can decline non-essential cookies
- **WHEN** a visitor declines non-essential cookies
- **THEN** only strictly necessary cookies are set and the banner is dismissed without loading Stripe JS

### Requirement: Privacy policy page exists at /privacy-policy
The system SHALL serve a static `/privacy-policy` page. The page SHALL cover: data controller identity, categories of data collected, legal basis for processing, retention periods, third-party processors (Stripe, Resend, Sanity, Azure), and UK GDPR rights.

#### Scenario: Privacy policy page is accessible
- **WHEN** a visitor navigates to `/privacy-policy`
- **THEN** a page is rendered with all required UK GDPR sections

### Requirement: Cookie policy page exists at /cookie-policy
The system SHALL serve a static `/cookie-policy` page listing all cookies set by the site, their purpose, and duration.

#### Scenario: Cookie policy page is accessible
- **WHEN** a visitor navigates to `/cookie-policy`
- **THEN** a page is rendered listing all cookies, their purpose, and duration

### Requirement: Terms and conditions page exists at /terms
The system SHALL serve a static `/terms` page covering: digital goods policy (no refunds on downloaded files), intellectual property, acceptable use, and governing law (England and Wales).

#### Scenario: Terms page is accessible
- **WHEN** a visitor navigates to `/terms`
- **THEN** a page is rendered with all required terms sections

### Requirement: No personal data collected from children
The system SHALL NOT knowingly collect personal data from users under 13. The privacy policy SHALL explicitly state that the site is directed at adults (parents/guardians) and that children should not submit personal information.

#### Scenario: Privacy policy states child data policy
- **WHEN** a visitor reads the privacy policy
- **THEN** a section explicitly states the site is for adults and that personal data from children under 13 is not knowingly collected

### Requirement: Newsletter opt-in is explicit and unambiguous
Newsletter signup SHALL use an unchecked checkbox with a clear label (see `newsletter-capture` spec). Pre-ticked or ambiguous consent is not permitted.

#### Scenario: Newsletter consent is separate from purchase
- **WHEN** a visitor completes a purchase
- **THEN** they are NOT automatically added to any marketing list

### Requirement: Footer links to legal pages on all pages
The site footer SHALL include links to `/privacy-policy`, `/cookie-policy`, and `/terms` on every page.

#### Scenario: Legal links present in footer
- **WHEN** any page is loaded
- **THEN** the footer contains links to Privacy Policy, Cookie Policy, and Terms
