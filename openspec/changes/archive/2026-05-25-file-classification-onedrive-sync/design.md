## Context

The OneDrive sync client downloads files via a delta-based pipeline. Each file passes through `DownloadJobBuilder` → `DownloadJobHandler` → `SyncedItemRegistrar`. The registrar is the single point where a downloaded file is persisted to the local SQLite DB (`SyncedItemEntity`). It already has the full remote path available.

Classification is purely additive: no existing pipeline stage changes behaviour. The sync client never acts on classifications — they exist solely for downstream querying.

## Goals / Non-Goals

**Goals:**
- Tokenise the remote path of every registered file and match against configured keyword rules
- Persist matching tags to a new `SyncedItemClassificationEntity` table
- Keep classification as a pure function — deterministic, no I/O, fully testable in isolation
- Zero performance impact on unchanged files (delta processing is already incremental)

**Non-Goals:**
- No UI for managing rules
- No per-account rule scoping
- No re-classify-all command
- No routing, priority, or conflict policy changes based on classification
- No changes to upload or delete paths

## Decisions

### 1. Classification runs in `SyncedItemRegistrar`, not `DownloadJobBuilder`

**Decision:** Classify at registration time, not job-build time.

**Rationale:** Classification is a persistence concern — it produces DB rows. `SyncedItemRegistrar` already owns DB writes for items. Putting classification there keeps it co-located with its output and avoids carrying unused data on `SyncJob` through the entire pipeline.

**Alternative considered:** Classify in `DownloadJobBuilder`, carry on `DownloadSyncJob`. Rejected because classification result is not needed during download execution, and it would bloat the job record with data irrelevant to the download itself.

### 2. `FileClassifier` is a pure static function, not a DI service

**Decision:** `FileClassifier.Classify(remotePath, rules)` — static method, no interface, no DI registration.

**Rationale:** Classification has no side effects and no dependencies. Making it an interface adds ceremony with no benefit. The rule list is injected into `SyncedItemRegistrar` as `IReadOnlyList<FileClassificationRule>` (bound from config), not wrapped in a service.

**Alternative considered:** `IFileClassifier` interface injected into registrar. Rejected — over-engineering for a pure function. Test by calling directly.

### 3. Tokenisation splits on `/`, `-`, `_`, `.`, ` ` with lowercase normalisation

**Decision:** Full path string split on all common delimiters, lowercased, empty tokens stripped and deduped.

**Rationale:** Covers both path-segment matching (`/Photos/Tuscany/`) and compound filename matching (`red-car-landscape.jpg`). File extension tokens (e.g. `jpg`) are included — rules can target extensions if desired.

**Alternative considered:** Split only on `/` (segment-only). Rejected — would not match `red-car.jpg` for keyword "red".

### 4. All matching rules fire — multiple tags per file

**Decision:** Evaluate every rule. Each matching rule contributes one `FileClassification`. Result is `IReadOnlyList<FileClassification>`.

**Rationale:** A file in `/Finance/Photos/` is legitimately both "Finance" and "Photos". Single-winner semantics would silently drop valid tags.

**Alternative considered:** First-match or longest-match-wins. Rejected — loses information without benefit given search is the only consumer.

### 5. No match → "Unclassified" sentinel tag

**Decision:** If no rules match, write a single tag with `Level1 = "Unclassified"`, `TagName = "Unclassified"`.

**Rationale:** Every file is findable. Search UI can show an "Unclassified" bucket without needing a special `LEFT JOIN / WHERE NULL` query.

### 6. `TagName` denormalised as most-specific level

**Decision:** `TagName` = `Level3 ?? Level2 ?? Level1`. Stored redundantly on the entity.

**Rationale:** Primary search query is `WHERE TagName = 'landscape'`. Avoids a `CASE` expression or application-side resolution on every query. Cost: one extra string column per tag row.

### 7. Classification is a snapshot — no automatic re-classification on rule change

**Decision:** Tags are written once at registration. If rules change, existing items keep old tags until they next appear in a delta.

**Rationale:** Simplest correct behaviour. Re-classify-all is a future command, not a required feature now. Predictable — users know what triggers re-classification.

## Risks / Trade-offs

- **Stale tags after rule change** → Acceptable for now; re-classify-all is a clear future addition. Document in config comments.
- **Token false positives** (e.g. keyword "car" matches file in `/Oscar/`) → Rules author controls keywords; keep keywords specific. Future: support phrase or path-anchored rules.
- **Extension tokens in keyword matches** (e.g. keyword "mp4" matched via extension) → Actually desirable — lets rules target file types. Document as a feature.
- **Large tag tables at scale** → Each file gets ≥1 tag row. At 300k files with avg 3 tags = ~900k rows. SQLite handles this comfortably with an index on `TagName`.

## Migration Plan

1. Add EF Core migration — creates `SyncedItemClassifications` table
2. Migration is additive — no existing data modified
3. Rollback: drop migration, remove table — no data loss on existing `SyncedItemEntity` rows
4. No config change required to run — `FileClassificationRules` defaults to empty; files get "Unclassified" tag
