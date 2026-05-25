## ADDED Requirements

### Requirement: Path tokenisation
The system SHALL tokenise a remote file path into a set of lowercase tokens by splitting on `/`, `-`, `_`, `.`, and space characters. Empty tokens SHALL be discarded. Duplicate tokens SHALL be deduplicated. The result SHALL be treated as a set (order-independent).

#### Scenario: Segment-based path
- **WHEN** the remote path is `/Photos/Tuscany/IMG_001.jpg`
- **THEN** tokens are `{ photos, tuscany, img_001, jpg }` (underscore within `img_001` also split, `img` and `001` separate tokens)

#### Scenario: Compound filename
- **WHEN** the remote path is `/docs/red-car-landscape.jpg`
- **THEN** tokens include `red`, `car`, `landscape`, and `jpg`

#### Scenario: Mixed separators
- **WHEN** the remote path is `/My_Documents/Finance Reports/Q1-2026.xlsx`
- **THEN** tokens include `my`, `documents`, `finance`, `reports`, `q1`, `2026`, `xlsx`

#### Scenario: Root-only path
- **WHEN** the remote path is `/somefile.txt`
- **THEN** tokens are `{ somefile, txt }`

### Requirement: Rule matching
The system SHALL evaluate every configured `FileClassificationRule` against the token set. A rule matches if at least one of its `Keywords` appears in the token set (case-insensitive comparison). Every matching rule SHALL contribute exactly one `FileClassification` to the result. Rules that do not match SHALL contribute nothing.

#### Scenario: Single rule matches
- **WHEN** tokens include `landscape` and one rule has keyword `landscape`
- **THEN** result contains that rule's classification

#### Scenario: Multiple rules match
- **WHEN** tokens include both `red` and `car` and there are separate rules for each
- **THEN** result contains both classifications

#### Scenario: No rules match
- **WHEN** no rule keyword appears in the token set
- **THEN** result contains exactly one classification with `Level1 = "Unclassified"` and `TagName = "Unclassified"`

#### Scenario: Rule with multiple keywords — partial match
- **WHEN** a rule has keywords `["car","vehicle","auto"]` and tokens contain `car` but not `vehicle` or `auto`
- **THEN** the rule matches and contributes its classification

#### Scenario: Empty rules list
- **WHEN** no `FileClassificationRules` are configured
- **THEN** result contains exactly one classification with `TagName = "Unclassified"`

### Requirement: Classification shape
Each `FileClassification` SHALL carry `Level1` (required string), `Level2` (optional string), `Level3` (optional string), and `IsSpecial` (bool). The `TagName` SHALL be computed as `Level3 ?? Level2 ?? Level1`.

#### Scenario: Three-level classification
- **WHEN** a rule specifies `Level1 = "Subject"`, `Level2 = "Vehicle"`, `Level3 = "Car"`
- **THEN** `TagName = "Car"`

#### Scenario: Two-level classification
- **WHEN** a rule specifies `Level1 = "Colour"`, `Level2 = "Red"`, no Level3
- **THEN** `TagName = "Red"`

#### Scenario: One-level classification
- **WHEN** a rule specifies only `Level1 = "Archive"`, no Level2 or Level3
- **THEN** `TagName = "Archive"`
