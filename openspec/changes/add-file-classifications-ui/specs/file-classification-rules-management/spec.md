## ADDED Requirements

### Requirement: List all classification rules
The UI SHALL display all persisted `FileClassificationRule` records in an observable list when the settings view is loaded. Each row SHALL show Keywords, Level1, Level2 (if present), Level3 (if present), and the IsSpecial flag.

#### Scenario: Rules exist in the database
- **WHEN** the settings view is loaded and rules exist in the database
- **THEN** all rules are displayed in the list, one row per rule

#### Scenario: No rules exist in the database
- **WHEN** the settings view is loaded and no rules exist
- **THEN** the list is empty and an empty-state message is shown

### Requirement: Add a classification rule
The UI SHALL provide a form with inputs for Keywords (comma-separated string), Level1 (required), Level2 (optional), Level3 (optional), and IsSpecial (checkbox). The Add command SHALL be disabled until Keywords and Level1 are non-empty. On confirmation the rule SHALL be persisted immediately and appear in the list without requiring a separate Save action.

#### Scenario: Add with required fields only
- **WHEN** the user enters keywords and Level1, then activates the Add command
- **THEN** the rule is persisted to the database and appended to the list

#### Scenario: Add with all fields
- **WHEN** the user enters keywords, Level1, Level2, Level3, and checks IsSpecial, then activates the Add command
- **THEN** the rule is persisted with all supplied values and appended to the list

#### Scenario: Add command disabled when Keywords is empty
- **WHEN** Keywords is empty (or whitespace-only)
- **THEN** the Add command is disabled

#### Scenario: Add command disabled when Level1 is empty
- **WHEN** Level1 is empty (or whitespace-only)
- **THEN** the Add command is disabled

#### Scenario: Form resets after successful add
- **WHEN** a rule is successfully added
- **THEN** all form inputs are cleared, ready for the next entry

#### Scenario: Keywords are split and trimmed
- **WHEN** the user enters `"photos, photo , img"` as Keywords
- **THEN** the persisted keywords list is `["photos", "photo", "img"]` (trimmed, no empty entries)

### Requirement: Delete a classification rule
Each rule row SHALL expose a delete action. Activating it SHALL remove the rule from the database immediately and remove it from the list without requiring a separate Save action.

#### Scenario: Delete an existing rule
- **WHEN** the user activates the delete action on a rule row
- **THEN** the rule is deleted from the database and removed from the list

#### Scenario: List reflects deletion immediately
- **WHEN** a rule is deleted
- **THEN** the list updates without requiring a page reload or Save
