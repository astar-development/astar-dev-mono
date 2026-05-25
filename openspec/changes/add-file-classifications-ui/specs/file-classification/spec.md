## MODIFIED Requirements

### Requirement: Rule persistence
The system SHALL provide a repository that supports reading, adding, and deleting `FileClassificationRule` records. `GetAllAsync` SHALL return all persisted rules. `AddAsync` SHALL persist a new rule from a domain record and return the assigned database `Id`. `DeleteAsync` SHALL remove the rule with the given `Id`; if no such rule exists the operation SHALL complete without error.

#### Scenario: Get all rules — rules exist
- **WHEN** `GetAllAsync` is called and rules exist in the database
- **THEN** all persisted rules are returned as `IReadOnlyList<FileClassificationRule>`

#### Scenario: Get all rules — empty database
- **WHEN** `GetAllAsync` is called and no rules exist
- **THEN** an empty list is returned

#### Scenario: Add a rule
- **WHEN** `AddAsync` is called with a valid `FileClassificationRule`
- **THEN** the rule is persisted and the assigned `int Id` is returned

#### Scenario: Delete an existing rule
- **WHEN** `DeleteAsync` is called with an `Id` that exists
- **THEN** the corresponding rule is removed from the database

#### Scenario: Delete a non-existent rule
- **WHEN** `DeleteAsync` is called with an `Id` that does not exist
- **THEN** the operation completes without throwing
