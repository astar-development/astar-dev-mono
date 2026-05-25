## 1. Domain Types

- [x] 1.1 Create `FileClassification` record (`Level1`, `Level2`, `Level3`, `IsSpecial`, computed `TagName`)
- [x] 1.2 Create `FileClassificationFactory` static factory with `Create` method
- [x] 1.3 Create `FileClassificationRule` record (`Keywords`, `FileClassification`)
- [x] 1.4 Create `FileClassificationRuleFactory` static factory with `Create` method

## 2. Classifier

- [x] 2.1 Create `FileClassifier` static class with `Classify(string remotePath, IReadOnlyList<FileClassificationRule> rules)` method
- [x] 2.2 Implement tokenisation: split on `/`, `-`, `_`, `.`, space; lowercase; strip empty; deduplicate
- [x] 2.3 Implement rule evaluation: all rules evaluated, each match contributes one tag
- [x] 2.4 Implement "Unclassified" sentinel when no rules match

## 3. Tests — Classifier

- [x] 3.1 Write failing tests for tokenisation scenarios (segment path, compound filename, mixed separators, root-only path)
- [x] 3.2 Write failing tests for rule matching scenarios (single match, multiple matches, no match, partial keyword match, empty rules)
- [x] 3.3 Write failing tests for `TagName` derivation (three-level, two-level, one-level)
- [x] 3.4 Run tests — confirm RED, commit failing tests
- [x] 3.5 Implement classifier to make tests GREEN

## 4. DB Entity & Migration

- [x] 4.1 Create `SyncedItemClassificationEntity` with properties: `Id`, `SyncedItemId` (FK), `Level1`, `Level2`, `Level3`, `TagName`, `IsSpecial`
- [x] 4.2 Register entity in `SyncClientDbContext` with index on `TagName`
- [x] 4.3 Add EF Core migration for new table
- [x] 4.4 Verify migration applies cleanly (`dotnet ef database update`)

## 5. Config Binding

- [x] 5.1 Create `FileClassificationRuleOptions` POCO for config binding (`Keywords`, `Level1`, `Level2`, `Level3`, `IsSpecial`)
- [x] 5.2 Register `FileClassificationRuleOptions` in DI/config pipeline bound to `FileClassificationRules` section
- [x] 5.3 Add representative example rules to `appsettings.json` (commented-out or dev-only)

## 6. Repository

- [x] 6.1 Add `UpsertClassificationsAsync(int syncedItemId, IReadOnlyList<FileClassification> classifications, CancellationToken ct)` to `ISyncedItemRepository`
- [x] 6.2 Implement: delete existing rows for `syncedItemId`, insert new rows

## 7. Registrar Integration

- [x] 7.1 Inject `IReadOnlyList<FileClassificationRule>` and repository classification method into `SyncedItemRegistrar`
- [x] 7.2 Call `FileClassifier.Classify(remotePath, rules)` and persist tags in `RegisterPhantomAsync` and any file registration path
- [x] 7.3 Confirm folder registration paths (`RegisterFolderAsync`) do NOT classify

## 8. Tests — Registrar Integration

- [x] 8.1 Write failing test: new file with matching rules → classification rows written
- [x] 8.2 Write failing test: new file with no matching rules → "Unclassified" row written
- [x] 8.3 Write failing test: re-registration replaces existing tags
- [x] 8.4 Write failing test: folder registration writes no classification rows
- [x] 8.5 Run tests — confirm RED, commit failing tests
- [x] 8.6 Implement registrar changes to make tests GREEN

## 9. Build & Verify

- [x] 9.1 `dotnet build` — zero errors, zero warnings
- [x] 9.2 `dotnet test` — all tests pass
