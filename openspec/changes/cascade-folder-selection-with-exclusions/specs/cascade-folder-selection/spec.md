## ADDED Requirements

### Requirement: Toggling a folder cascades state to all loaded descendants
When a user toggles a folder's sync state, the system SHALL apply the new state to every already-loaded descendant node in the tree.

#### Scenario: Include cascades to loaded children
- **WHEN** a folder in `Excluded` state is toggled to `Included`
- **THEN** every loaded child and grandchild node transitions to `Included`

#### Scenario: Exclude cascades to loaded children
- **WHEN** a folder in `Included` state is toggled to `Excluded`
- **THEN** every loaded child and grandchild node transitions to `Excluded`

#### Scenario: Cascade does not load unloaded sub-trees
- **WHEN** a folder is toggled and some children have never been expanded
- **THEN** only already-loaded descendants change state; no network call is made to load unloaded children

### Requirement: Lazily-loaded children inherit the parent's sync state
When a folder's children are loaded on expand, the system SHALL initialise each child's `SyncState` from the parent's current inherited state rather than always defaulting to `Excluded`.

#### Scenario: Children of an included folder start as included
- **WHEN** the user expands a folder whose `SyncState` is `Included`
- **THEN** each newly loaded child starts with `SyncState = Included`

#### Scenario: Children of an excluded folder start as excluded
- **WHEN** the user expands a folder whose `SyncState` is `Excluded`
- **THEN** each newly loaded child starts with `SyncState = Excluded`

#### Scenario: Inherited state overridden by persisted explicit exclusion
- **WHEN** a child folder has a persisted explicit exclusion and the parent is `Included`
- **THEN** the child starts with `SyncState = Excluded` despite the parent's inherited state

### Requirement: Parent folder reflects mixed child states as Partial
When a folder's descendants have mixed inclusion, the system SHALL display the parent's state as `Partial`.

#### Scenario: Parent becomes Partial when a child is excluded
- **WHEN** one or more children of an `Included` folder are toggled to `Excluded`
- **THEN** the parent node transitions to `SyncState = Partial`

#### Scenario: Parent returns to Included when all children are included
- **WHEN** all children of a `Partial` folder are toggled back to `Included`
- **THEN** the parent node transitions to `SyncState = Included`

#### Scenario: Parent returns to Excluded when all children are excluded
- **WHEN** all children of a `Partial` folder are toggled to `Excluded`
- **THEN** the parent node transitions to `SyncState = Excluded`
