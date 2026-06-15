## ADDED Requirements

### Requirement: Name filter
The system SHALL filter synced file results to those whose filename (the final path segment of `RemotePath`) contains the supplied name fragment, case-insensitively. When no name fragment is provided the name filter SHALL be inactive.

#### Scenario: Partial name match
- **WHEN** the user enters `"holiday"` in the name field
- **THEN** only files whose filename contains `"holiday"` (case-insensitive) appear in results

#### Scenario: Empty name field
- **WHEN** the name field is empty
- **THEN** the name filter is inactive and does not restrict results

#### Scenario: No matches
- **WHEN** the user enters a name fragment that matches no synced filenames
- **THEN** the results list is empty and a "No results" message is shown

### Requirement: Size range filter
The system SHALL filter results to files whose `SizeInBytes` falls within an inclusive [min, max] range. Either bound MAY be left blank, in which case that bound is unbounded. Files with `SizeInBytes` of `null` (not yet populated) SHALL be excluded when any size bound is active.

#### Scenario: Minimum size only
- **WHEN** the user sets minimum size to 10 MB and leaves maximum blank
- **THEN** only files with `SizeInBytes >= 10,485,760` are returned

#### Scenario: Maximum size only
- **WHEN** the user sets maximum size to 1 MB and leaves minimum blank
- **THEN** only files with `SizeInBytes <= 1,048,576` are returned

#### Scenario: Both bounds set
- **WHEN** the user sets minimum to 1 MB and maximum to 10 MB
- **THEN** only files with `1,048,576 <= SizeInBytes <= 10,485,760` are returned

#### Scenario: Null size excluded when filter active
- **WHEN** any size bound is set
- **THEN** files with `SizeInBytes = null` do not appear in results

### Requirement: Classification tag filter
The system SHALL filter results to files that carry at least one `SyncedItemClassificationEntity` row whose `TagName` matches any of the selected tags. When no tags are selected the classification filter SHALL be inactive.

#### Scenario: Single tag selected
- **WHEN** the user selects the tag `"Holiday"`
- **THEN** only files that have a classification with `TagName = "Holiday"` are returned

#### Scenario: Multiple tags selected (OR within tag filter)
- **WHEN** the user selects tags `"Holiday"` and `"Work"`
- **THEN** files that have a classification matching `"Holiday"` OR `"Work"` are returned

#### Scenario: No tags selected
- **WHEN** no tags are selected
- **THEN** the classification filter is inactive and does not restrict results

### Requirement: Duplicates-only filter
The system SHALL identify duplicate files as two or more non-folder `SyncedItemEntity` rows belonging to the same account that share identical filename (final segment of `RemotePath`) and `SizeInBytes`. When the "duplicates only" toggle is active only such files SHALL appear in results.

#### Scenario: Toggle on — duplicates present
- **WHEN** the duplicates toggle is active and two files share the same name and size
- **THEN** both files appear in results

#### Scenario: Toggle on — no duplicates
- **WHEN** the duplicates toggle is active and no files share a name and size
- **THEN** the results list is empty

#### Scenario: Toggle off
- **WHEN** the duplicates toggle is inactive
- **THEN** the duplicate condition does not restrict results

### Requirement: Combined filter (AND semantics)
The system SHALL apply all active criteria simultaneously using AND logic: a file appears in results only when it satisfies every active filter.

#### Scenario: Name and classification combined
- **WHEN** the user enters name `"beach"` and selects tag `"Holiday"`
- **THEN** only files whose filename contains `"beach"` AND that carry a `"Holiday"` classification are returned

#### Scenario: Size and duplicates combined
- **WHEN** the user sets minimum size to 5 MB and enables the duplicates toggle
- **THEN** only files that are duplicates AND have `SizeInBytes >= 5,242,880` are returned

### Requirement: Result count badge
The system SHALL display the count of matching results adjacent to the search criteria panel. The count SHALL update each time the active criteria change and after each search execution.

#### Scenario: Count reflects filter results
- **WHEN** a search returns 42 files
- **THEN** the badge displays `"42"`

#### Scenario: Count on empty results
- **WHEN** no files match the active criteria
- **THEN** the badge displays `"0"`

### Requirement: Folder exclusion
The system SHALL exclude folder entries (`IsFolder = true`) from all search results regardless of other filter criteria.

#### Scenario: Folders not returned
- **WHEN** the user searches with no filters active
- **THEN** `SyncedItemEntity` rows where `IsFolder = true` do not appear in results
