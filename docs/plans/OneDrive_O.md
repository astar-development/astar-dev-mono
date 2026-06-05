# Plan: FileClassificationRules — Proper Hierarchy (OneDrive_O)

## Context

`AStar.Dev.OneDrive.Sync.Client` stores `FileClassificationRules` in SQLite as a **flat list**. Each row holds
pipe-delimited `Keywords` plus three denormalised string columns (`Level1` NOT NULL, `Level2?`, `Level3?`) and a
single `IsSpecial` flag covering the whole rule.

Problems:
- Category strings duplicated across rows; no referential integrity between levels.
- No single source of truth for the category tree.
- `IsSpecial` is coarse — applies to the entire rule, not to an individual keyword.

This plan replaces the flat table with a **proper 3-level category hierarchy** plus a separate keyword table.

### Two classifiers run today

The sync pipeline already combines **two** sources, appending both into `SyncedItemClassifications`:

1. **`FileClassifier.Classify(path, dbRules)`** — matches the (flat) DB rule table; emits an `[Unclassified]`
   sentinel when nothing matches. This is the table being made hierarchical.
2. **`RuleBasedFileAutoCategorisor.Categorise(path)`** (`IFileAutoCategorisor`) — heuristic engine built on
   `TokenAnalyser` (person-name + colour-phrase extraction) and `Level1Deriver.FolderTypeMap`. Derives
   `Person` / `Color` / `Place` / `Event` / `Object` Level1 plus Level2/3, always returns one result.

Call sites: [`SyncedItemRegistrar`](../../apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/Jobs/SyncedItemRegistrar.cs)
and [`SyncJobExecutor`](../../apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/Pipeline/SyncJobExecutor.cs).
This plan **keeps** the analyser and combines the two cleanly (see *Classification Pipeline & Combination*).

### Decisions agreed in planning

| Question | Decision |
|---|---|
| Hierarchy shape | **Fixed 3 levels**: Level1 → Level2 → Level3 (self-referencing category table, level enforced 1–3) |
| Keyword placement | **Leaf node only** — a keyword attaches to the deepest assigned node of its branch (a node with no children) |
| IsSpecial | **Category node + keyword override** — flag on category node; keyword override `None` = inherit, `Some(bool)` = override |
| Migration | **Transform existing data** — EF Core migration rebuilds the tree from existing rows; no drop-and-reseed, no data loss |
| TokenAnalyser combine | **Union, drop sentinel** — DB-rule matches and the `TokenAnalyser` heuristic both contribute; `FileClassifier` stops emitting the `Unclassified` sentinel, the combiner owns the fallback |
| Analyser vocab | **Keep hardcoded** — `StopWords`/`ColourWords`/`ConcretePairableNouns`/`AbstractNouns`/`FolderTypeMap` stay as `FrozenSet`s in code |
| Analyser categories | **Map to hierarchy L1** — `Person`, `Color`, `Place`, `Event`, `Object` are seeded as Level 1 category nodes so both sources share one vocabulary |

---

## Current State

### Table

```
FileClassificationRules (Id PK, Keywords TEXT NOT NULL, Level1 TEXT NOT NULL, Level2 TEXT?, Level3 TEXT?, IsSpecial INTEGER)
```

`Keywords` is a `|`-delimited string per row (e.g. `"photos|photo|img"`).

### Key files

| Role | Path |
|---|---|
| EF entity | `Data/Entities/FileClassificationRuleEntity.cs` |
| EF config | `Data/Configuration/FileClassificationRuleEntityConfiguration.cs` |
| DbContext | `Data/AppDbContext.cs` |
| Repository interface | `Data/Repositories/IFileClassificationRuleRepository.cs` |
| Repository impl | `Data/Repositories/FileClassificationRuleRepository.cs` |
| Domain rule | `Domain/FileClassificationRule.cs` |
| Domain entry | `Domain/FileClassificationRuleEntry.cs` |
| Domain classification (result) | `Domain/FileClassification.cs` |
| Classifier | `Domain/FileClassifier.cs` |
| Rule factory | `Domain/FileClassificationRuleFactory.cs` |
| Classification factory | `Domain/FileClassificationFactory.cs` |
| Row VM | `Classifications/FileClassificationRuleRowViewModel.cs` |
| List VM | `Classifications/FileClassificationRulesViewModel.cs` |

`FileClassification` / `FileClassificationFactory` are the **result** of classifying a file (its tags), not rule
storage — they stay unchanged.

---

## Target State

### New tables

```sql
-- The 3-level category tree
FileClassificationCategories
  Id          INTEGER  PRIMARY KEY AUTOINCREMENT
  Name        TEXT     NOT NULL
  Level       INTEGER  NOT NULL          -- 1, 2 or 3; validated in factory
  ParentId    INTEGER  NULL              -- FK -> FileClassificationCategories.Id (NULL only for Level 1)
  IsSpecial   INTEGER  NOT NULL DEFAULT 0

-- One row per keyword; keyword attaches to a LEAF category node only
FileClassificationKeywords
  Id          INTEGER  PRIMARY KEY AUTOINCREMENT
  Keyword     TEXT     NOT NULL
  CategoryId  INTEGER  NOT NULL          -- FK -> FileClassificationCategories.Id ON DELETE CASCADE
  IsSpecial   INTEGER  NULL              -- NULL = inherit Category.IsSpecial; 0/1 = override
```

`FileClassificationRules` is **dropped** by the migration after data is transferred.

### Constraints (enforced in factory + EF config; not SQL CHECK, for SQLite portability)

- `Level = 1` → `ParentId` must be `NULL`.
- `Level = 2` → parent node must have `Level = 1`.
- `Level = 3` → parent node must have `Level = 2`.
- Category `Name` unique among siblings → unique index on `(ParentId, Name)`.
- `Keyword` must be non-empty, non-whitespace.
- **Leaf rule**: a keyword's `CategoryId` must reference a node with **no child categories**. Enforced in the
  repository on add/update (and preserved by migration backfill). A category that has children cannot own
  keywords; conversely adding a child to a category that owns keywords is rejected.

---

## EF Core Entities

### `FileClassificationCategoryEntity`

```csharp
// Data/Entities/FileClassificationCategoryEntity.cs
public sealed class FileClassificationCategoryEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int? ParentId { get; set; }
    public FileClassificationCategoryEntity? Parent { get; set; }
    public ICollection<FileClassificationCategoryEntity> Children { get; set; } = [];
    public ICollection<FileClassificationKeywordEntity> Keywords { get; set; } = [];
    public bool IsSpecial { get; set; }
}
```

### `FileClassificationKeywordEntity`

```csharp
// Data/Entities/FileClassificationKeywordEntity.cs
public sealed class FileClassificationKeywordEntity
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public FileClassificationCategoryEntity Category { get; set; } = null!;
    public bool? IsSpecial { get; set; }  // null = inherit from Category.IsSpecial
}
```

`FileClassificationRuleEntity` is **deleted** once migration and all call-site updates are complete.

---

## EF Core Configurations

### `FileClassificationCategoryEntityConfiguration`

```csharp
// Data/Configuration/FileClassificationCategoryEntityConfiguration.cs
public sealed class FileClassificationCategoryEntityConfiguration : IEntityTypeConfiguration<FileClassificationCategoryEntity>
{
    public void Configure(EntityTypeBuilder<FileClassificationCategoryEntity> builder)
    {
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).IsRequired();
        builder.Property(category => category.Level).IsRequired();
        builder.HasIndex(category => new { category.ParentId, category.Name }).IsUnique();
        builder.HasOne(category => category.Parent)
               .WithMany(category => category.Children)
               .HasForeignKey(category => category.ParentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### `FileClassificationKeywordEntityConfiguration`

```csharp
// Data/Configuration/FileClassificationKeywordEntityConfiguration.cs
public sealed class FileClassificationKeywordEntityConfiguration : IEntityTypeConfiguration<FileClassificationKeywordEntity>
{
    public void Configure(EntityTypeBuilder<FileClassificationKeywordEntity> builder)
    {
        builder.HasKey(keyword => keyword.Id);
        builder.Property(keyword => keyword.Keyword).IsRequired();
        builder.HasOne(keyword => keyword.Category)
               .WithMany(category => category.Keywords)
               .HasForeignKey(keyword => keyword.CategoryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Delete `FileClassificationRuleEntityConfiguration`.

---

## DbContext Changes

```csharp
// AppDbContext.cs
public DbSet<FileClassificationCategoryEntity> FileClassificationCategories => Set<FileClassificationCategoryEntity>();
public DbSet<FileClassificationKeywordEntity> FileClassificationKeywords => Set<FileClassificationKeywordEntity>();

// Delete: public DbSet<FileClassificationRuleEntity> FileClassificationRules => ...
```

---

## Domain Model Changes

### New records

```csharp
// Domain/FileClassificationCategory.cs
public sealed record FileClassificationCategory(int Id, string Name, int Level, Option<int> ParentId, bool IsSpecial);

// Domain/FileClassificationKeyword.cs
public sealed record FileClassificationKeyword(int Id, string Keyword, int CategoryId, Option<bool> IsSpecialOverride);
```

`IsSpecialOverride` semantics:
- `Option.None` → inherit owning `FileClassificationCategory.IsSpecial`.
- `Option.Some(true/false)` → explicit override.

### Extension methods (no methods on records)

```csharp
// Domain/FileClassificationKeywordExtensions.cs
public static class FileClassificationKeywordExtensions
{
    public static bool ResolveIsSpecial(this FileClassificationKeyword keyword, bool categoryIsSpecial)
        => keyword.IsSpecialOverride.MapOrDefault(value => value, categoryIsSpecial);
}
```

### Factories (each `record Foo` gets a `FooFactory` returning `Result<T, string>`)

**`FileClassificationCategoryFactory`**
- `Level` must be 1–3.
- `Level = 1` → `ParentId` must be `None`.
- `Level > 1` → `ParentId` must be `Some`.
- `Name` non-empty, non-whitespace (normalise/trim).

**`FileClassificationKeywordFactory`**
- `Keyword` non-empty, non-whitespace (trim, lower-case to match classifier tokenisation).

### Retire

| Item | Reason |
|---|---|
| `FileClassificationRule` | keyword→category FK replaces "keywords + classification in one row" |
| `FileClassificationRuleEntry` | `FileClassificationKeyword` carries its own `Id` |
| `FileClassificationRuleFactory` | replaced by `FileClassificationKeywordFactory` |

`FileClassification` + `FileClassificationFactory` — **unchanged** (classification result, not rule storage).

---

## Classifier Changes

`FileClassifier.Classify` currently takes `IReadOnlyList<FileClassificationRule>`. New signature consumes the
flat keyword→category mappings (keywords live on leaf nodes; the full Level1/2/3 path is resolved by walking
`Category → Parent → Parent`).

```csharp
public static IReadOnlyList<FileClassification> Classify(
    string remotePath,
    IReadOnlyList<KeywordMapping> mappings);
```

where `KeywordMapping` carries the keyword plus its resolved Level1/Level2/Level3 names and effective
`IsSpecial`. Matching logic (tokenise path, match keyword against tokens) is unchanged; only the rule shape and
the `FileClassification` construction change. Effective `IsSpecial` = `keyword.ResolveIsSpecial(leafCategory.IsSpecial)`.

**Behaviour change — drop the sentinel.** `Classify` now returns an **empty list** when no mapping matches. The
`Unclassified` fallback moves to the combiner (below) so it can see whether the analyser also produced nothing.

---

## Classification Pipeline & Combination

`TokenAnalyser` and `RuleBasedFileAutoCategorisor` are **kept unchanged** — their vocab stays as hardcoded
`FrozenSet`s. A new combiner unions the two sources with the **Union, drop sentinel** policy.

```csharp
// Domain/ClassificationCombiner.cs
public static class ClassificationCombiner
{
    public static IReadOnlyList<FileClassification> Combine(
        IReadOnlyList<FileClassification> ruleMatches,   // FileClassifier output (may be empty)
        FileClassification analyserResult)               // RuleBasedFileAutoCategorisor output (always present)
    {
        var combined = ruleMatches
            .Append(analyserResult)
            .DistinctBy(classification => (classification.Level1, classification.Level2, classification.Level3))
            .ToList();

        return combined.Count == 0
            ? [FileClassificationFactory.CreateUnclassified()]
            : combined.AsReadOnly();
    }
}
```

- The `Unclassified` sentinel now fires **only** if both sources are empty (in practice the analyser always
  yields at least `Object`, so it is a true safety net).
- De-dupe on the `(Level1, Level2, Level3)` triple so a rule match and an identical analyser result collapse.
- Because the analyser's Level1 values (`Person`/`Color`/`Place`/`Event`/`Object`) are **seeded as Level 1
  category nodes** by the migration, both sources speak the same Level1 vocabulary and de-dupe correctly.

Call sites `SyncedItemRegistrar` and `SyncJobExecutor` change from
`FileClassifier.Classify(...).Append(autoCategorisor.Categorise(...))` to
`ClassificationCombiner.Combine(FileClassifier.Classify(path, mappings), autoCategorisor.Categorise(path))`.

---

## Repository

### Interface (new file)

```csharp
// Data/Repositories/IFileClassificationRepository.cs
public interface IFileClassificationRepository
{
    Task<IReadOnlyList<FileClassificationCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileClassificationKeyword>> GetKeywordsForCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    // Flat projection for the classifier: keyword + its resolved category path, for every keyword row
    Task<IReadOnlyList<KeywordMapping>> GetAllKeywordMappingsAsync(CancellationToken cancellationToken = default);

    Task<Result<int, string>> AddCategoryAsync(FileClassificationCategory category, CancellationToken cancellationToken = default);
    Task<Result<int, string>> AddKeywordAsync(FileClassificationKeyword keyword, CancellationToken cancellationToken = default);
    Task<Result<Unit, string>> UpdateCategoryAsync(int id, FileClassificationCategory category, CancellationToken cancellationToken = default);
    Task<Result<Unit, string>> UpdateKeywordAsync(int id, FileClassificationKeyword keyword, CancellationToken cancellationToken = default);
    Task<Result<Unit, string>> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Unit, string>> DeleteKeywordAsync(int id, CancellationToken cancellationToken = default);
}
```

### Key implementation notes

- **Leaf enforcement** lives here: `AddKeywordAsync` / `UpdateKeywordAsync` reject a `CategoryId` that has child
  categories; `AddCategoryAsync` rejects a `ParentId` that already owns keywords. Return `Result.Error`, no throw.
- `GetAllKeywordMappingsAsync` loads `FileClassificationKeywords` with `.Include(k => k.Category).ThenInclude(c => c.Parent)...`
  (walk up to L1) and projects to `KeywordMapping` (keyword text, Level1/2/3 names, effective `IsSpecial`).
- DI: swap `IFileClassificationRuleRepository` → `IFileClassificationRepository` registration in
  `Data/PersistenceServiceExtensions.cs`; update every call site.

---

## EF Core Migration

Migration name: `HierarchicalFileClassificationRules`. SQLite has no native string-split, so keyword splitting
is done in C# inside the migration `Up` using a raw `DbConnection` (open the connection
`migrationBuilder` runs against; read old rows; issue parameterised INSERTs).

### Up — logical steps

**1. Create new tables** (`CreateTable` for both, including indexes + FKs).

**2. Seed Level 1** — distinct non-empty `Level1`:

```sql
INSERT INTO FileClassificationCategories (Name, Level, ParentId, IsSpecial)
SELECT Level1, 1, NULL, MAX(CAST(IsSpecial AS INTEGER))
FROM FileClassificationRules
WHERE Level1 IS NOT NULL AND Level1 != ''
GROUP BY Level1;
```

**3. Seed Level 2** — distinct `Level1+Level2`:

```sql
INSERT INTO FileClassificationCategories (Name, Level, ParentId, IsSpecial)
SELECT r.Level2, 2, l1.Id, MAX(CAST(r.IsSpecial AS INTEGER))
FROM FileClassificationRules r
JOIN FileClassificationCategories l1 ON l1.Name = r.Level1 AND l1.Level = 1
WHERE r.Level2 IS NOT NULL AND r.Level2 != ''
GROUP BY r.Level2, l1.Id;
```

**4. Seed Level 3** — distinct `Level1+Level2+Level3`:

```sql
INSERT INTO FileClassificationCategories (Name, Level, ParentId, IsSpecial)
SELECT r.Level3, 3, l2.Id, MAX(CAST(r.IsSpecial AS INTEGER))
FROM FileClassificationRules r
JOIN FileClassificationCategories l1 ON l1.Name = r.Level1 AND l1.Level = 1
JOIN FileClassificationCategories l2 ON l2.Name = r.Level2 AND l2.ParentId = l1.Id AND l2.Level = 2
WHERE r.Level3 IS NOT NULL AND r.Level3 != ''
GROUP BY r.Level3, l2.Id;
```

**5. Migrate keywords (C#)** — for each old rule, resolve its **leaf** category (deepest non-null level =
the leaf for that branch), split `Keywords` on `|`, insert one keyword row per token against that leaf:

```csharp
foreach (var oldRule in QueryOldRules(connection))
{
    var leafCategoryId = ResolveLeafCategoryId(connection, oldRule); // deepest of L3 ?? L2 ?? L1
    foreach (var token in oldRule.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries))
        InsertKeyword(connection, token.Trim().ToLowerInvariant(), leafCategoryId, isSpecial: null);
    // isSpecial null: rule-level IsSpecial already promoted onto the category node (steps 2-4)
}
```

The leaf rule holds automatically: keywords only ever attach to the deepest assigned node of a branch.

**6. Seed analyser Level 1 nodes** — idempotently add the categoriser's derived Level1 values so both sources
share one tree. Insert only names not already present at Level 1 (from step 2):

```sql
INSERT INTO FileClassificationCategories (Name, Level, ParentId, IsSpecial)
SELECT seed.Name, 1, NULL, 0
FROM (SELECT 'Person' AS Name UNION SELECT 'Color' UNION SELECT 'Place'
      UNION SELECT 'Event' UNION SELECT 'Object') seed
WHERE NOT EXISTS (
    SELECT 1 FROM FileClassificationCategories existing
    WHERE existing.Level = 1 AND existing.ParentId IS NULL AND existing.Name = seed.Name);
```

These names mirror `Level1Deriver.FolderTypeMap` + the colour/person/object fallbacks in
`RuleBasedFileAutoCategorisor`. The analyser still derives them in code; seeding only guarantees a matching
editable node exists so de-dupe in the combiner aligns. (`SyncedItemClassifications` rows remain string-only;
no FK back to the tree — see *Out of Scope*.)

**7. Drop old table**: `DROP TABLE FileClassificationRules;`

### Down (rollback)

Reconstructing pipe-delimited rows from normalised data is lossy/complex. Mark unsupported; rollback = restore
from a pre-migration backup.

```csharp
protected override void Down(MigrationBuilder migrationBuilder)
    => throw new NotSupportedException("HierarchicalFileClassificationRules cannot be rolled back automatically. Restore from backup.");
```

---

## UI / ViewModel Impact

`FileClassificationRulesViewModel` + `FileClassificationRuleRowViewModel` currently bind a flat grid of rules.
They must become a tree (categories) + keyword editor. Scope note: this plan covers the **data + domain +
migration**. A follow-up issue should cover the full tree-edit UX (add/move/delete category nodes, assign
keywords to leaves, toggle `IsSpecial` at node + keyword). Minimum here: keep the app compiling and able to
list/add/delete categories and leaf keywords. Avalonia tree binding follows the ScrollViewer bounded-viewport
rule in `.claude/rules/avalonia-ui.md`.

---

## Files to Create / Modify / Delete

### Create
- `Data/Entities/FileClassificationCategoryEntity.cs`
- `Data/Entities/FileClassificationKeywordEntity.cs`
- `Data/Configuration/FileClassificationCategoryEntityConfiguration.cs`
- `Data/Configuration/FileClassificationKeywordEntityConfiguration.cs`
- `Data/Repositories/IFileClassificationRepository.cs`
- `Data/Repositories/FileClassificationRepository.cs`
- `Domain/FileClassificationCategory.cs`
- `Domain/FileClassificationKeyword.cs`
- `Domain/FileClassificationCategoryFactory.cs`
- `Domain/FileClassificationKeywordFactory.cs`
- `Domain/FileClassificationKeywordExtensions.cs`
- `Domain/KeywordMapping.cs` (+ `KeywordMappingFactory.cs`)
- `Domain/ClassificationCombiner.cs`
- `Data/Migrations/<timestamp>_HierarchicalFileClassificationRules.cs`

### Modify
- `Data/AppDbContext.cs` — swap DbSets
- `Domain/FileClassifier.cs` — new signature / mapping input; **drop the `Unclassified` sentinel** (return empty)
- `Infrastructure/Sync/Jobs/SyncedItemRegistrar.cs` — use `ClassificationCombiner.Combine(...)`
- `Infrastructure/Sync/Pipeline/SyncJobExecutor.cs` — use `ClassificationCombiner.Combine(...)`
- `Data/PersistenceServiceExtensions.cs` — DI swap
- `Classifications/FileClassificationRulesViewModel.cs` + `FileClassificationRuleRowViewModel.cs` — tree binding
- Any other call site of `IFileClassificationRuleRepository` (services, tests)

`TokenAnalyser`, `RuleBasedFileAutoCategorisor`, `Level1Deriver` — **unchanged**.

### Delete (after migration applied + call sites updated)
- `Data/Entities/FileClassificationRuleEntity.cs`
- `Data/Configuration/FileClassificationRuleEntityConfiguration.cs`
- `Data/Repositories/IFileClassificationRuleRepository.cs`
- `Data/Repositories/FileClassificationRuleRepository.cs`
- `Domain/FileClassificationRule.cs`
- `Domain/FileClassificationRuleEntry.cs`
- `Domain/FileClassificationRuleFactory.cs`

---

## Out of Scope

`SyncedItemClassifications` stores the *result* of classifying a file (denormalised Level1/2/3 strings) from
**both** sources (DB rules + analyser). It stays **string-only** — no FK to `FileClassificationCategories`.
Adding referential integrity there (and back-linking historical rows to tree nodes) is a separate issue.

`TokenAnalyser` and `RuleBasedFileAutoCategorisor` internals — kept **as-is**, vocab stays hardcoded. This plan
only changes how their output is *combined* with DB-rule output (the combiner) and seeds matching Level1 nodes.

Full tree-edit UX (drag/move nodes, rich keyword editor) — follow-up issue.

---

## Testing Plan (TDD: factories → extensions → classifier → repository → migration)

| Test class | Covers |
|---|---|
| `GivenFileClassificationCategoryFactory` | Level 1–3 validation, L1 forbids ParentId, L>1 requires ParentId, name trim/empty reject |
| `GivenFileClassificationKeywordFactory` | empty/whitespace keyword rejected; valid keyword trimmed + lowercased |
| `GivenFileClassificationKeywordExtensions` | `ResolveIsSpecial` — 4 combos (category ±, override None/Some) |
| `GivenAFileClassifier` | path tokenised, keyword match builds correct Level1/2/3 path + effective IsSpecial; **returns empty (no sentinel) on no match** |
| `GivenAClassificationCombiner` | union of rule + analyser results; de-dupe on (L1,L2,L3) triple; `Unclassified` only when both empty; analyser result always retained |
| `GivenFileClassificationRepository` | CRUD both tables; **leaf enforcement** (reject keyword on non-leaf, reject child under keyword-owning node); `GetAllKeywordMappingsAsync` projection; cascade delete |
| `GivenHierarchicalMigration` (integration) | old flat rules round-trip into tree; keyword count = token count; keywords land on leaf; effective IsSpecial preserved; **analyser L1 nodes (Person/Color/Place/Event/Object) seeded once, no duplicates** |

---

## Phased Delivery

**Invariant: `main` is buildable, test-green and deployable after _every_ phase.** Pattern is
expand → migrate → contract (parallel-change): new structures land additively and unused first, the pipeline
cuts over atomically, then the old structures are removed. Each phase changes/creates **≤ 6 files (tests
included)** and ships as one PR. One GitHub issue per phase (raised on approval, per repo convention).

Old and new code coexist from Phase 4 until Phase 9 — that overlap is what keeps `main` deployable.

| # | Goal | Files (count) | Why `main` stays deployable |
|---|---|---|---|
| **1** | Category domain | `Domain/FileClassificationCategory.cs`, `…CategoryFactory.cs`, `Tests/…/GivenAFileClassificationCategoryFactory.cs` **(3)** | Pure additive; nothing references it yet |
| **2** | Keyword domain | `Domain/FileClassificationKeyword.cs`, `…KeywordFactory.cs`, `…KeywordExtensions.cs`, `Tests/…/GivenAFileClassificationKeywordFactory.cs`, `…/GivenFileClassificationKeywordExtensions.cs` **(5)** | Additive; unused |
| **3** | Mapping + combiner | `Domain/KeywordMapping.cs`, `…KeywordMappingFactory.cs`, `Domain/ClassificationCombiner.cs`, `Tests/…/GivenAClassificationCombiner.cs` **(4)** | Combiner compiled but not wired into pipeline |
| **4** | EF entities + configs | 2 entities + 2 configs **(4)** | Not yet mapped in `AppDbContext` → no migration drift; compiles |
| **5** | DbSets + create/seed/backfill migration | `Data/AppDbContext.cs` (+2 DbSets), migration `.cs` + `.Designer.cs`, `AppDbContextModelSnapshot.cs` **(4)** | New tables created **alongside** the old; seeds L1/2/3 + keywords + analyser L1 nodes. **No drop.** Old table + flow untouched |
| **6** | New repository (registered alongside old) | `Data/Repositories/IFileClassificationRepository.cs`, `FileClassificationRepository.cs`, `Data/PersistenceServiceExtensions.cs` (add reg, keep old), `Tests/…/GivenAFileClassificationRepository.cs` **(4)** | Both repos registered; nothing consumes the new one yet |
| **7** | Classifier overload + **pipeline cutover** | `Domain/FileClassifier.cs` (add `Classify(path, mappings)`, keep old overload), `Infrastructure/Sync/Jobs/SyncedItemRegistrar.cs`, `Infrastructure/Sync/Pipeline/SyncJobExecutor.cs`, `Tests/…/GivenAFileClassifier.cs`, `…/GivenASyncedItemRegistrar.cs`, `…/GivenASyncJobExecutor.cs` **(6)** | Atomic: pipeline switches to new repo + `ClassificationCombiner`. New repo (Ph6) resolves the new ctor deps. Old repo now only used by the VM |
| **8** | VM cutover to the tree | `Classifications/FileClassificationRulesViewModel.cs`, `FileClassificationRuleRowViewModel.cs`, `FileClassificationsView.axaml` (+`.axaml.cs`), `App.axaml.cs` (design-time DI), `Tests/…/GivenAFileClassificationRulesViewModel.cs` **(≤6)** | After this **nothing** references `IFileClassificationRuleRepository` but its own interface/impl. If a `CategoryNodeViewModel` is needed, split into 8a/8b to stay ≤6 |
| **9** | Remove old repository + old classifier overload | `Data/PersistenceServiceExtensions.cs` (drop old reg), **delete** `IFileClassificationRuleRepository.cs`, `FileClassificationRuleRepository.cs`, `Tests/…/GivenAFileClassificationRuleRepository.cs`, `Domain/FileClassifier.cs` (remove old overload) **(5)** | Old repo unreferenced since Ph8; safe to delete. `FileClassificationRule`/`Entry`/`Factory` now unused |
| **10** | Remove old rule domain | **delete** `Domain/FileClassificationRule.cs`, `FileClassificationRuleEntry.cs`, `FileClassificationRuleFactory.cs` **(3)** | Unreferenced after Ph9 |
| **11** | Drop old table + entity | `Data/AppDbContext.cs` (remove old DbSet), **delete** `FileClassificationRuleEntity.cs` + `FileClassificationRuleEntityConfiguration.cs`, drop-table migration `.cs` + `.Designer.cs`, `AppDbContextModelSnapshot.cs` **(6)** | Data copied in Ph5; no code reads the old table since Ph7/8. Drop migration **re-runs the idempotent backfill first** to capture any rows the old UI wrote between Ph5 and Ph8, then drops. Model clean |

### Sequencing guarantees

- **Migrations are additive until Phase 11.** Ph5 only creates/seeds; the destructive `DROP TABLE` is last, after
  every reader is gone.
- **Late-write safety.** Old UI can still write the old table between Ph5 and Ph8. The backfill is **idempotent**
  (`WHERE NOT EXISTS` / keyword de-dupe) and is **re-executed inside the Ph11 drop migration** so no rule is lost.
- **Per-phase gate (Definition of Done below) runs every phase** — a phase merges only on zero-warning build +
  zero new test failures, keeping `main` deployable.

---

## Definition of Done

### Per phase (every PR)

- [ ] `dotnet build` — zero errors, zero warnings (paste exact output)
- [ ] `dotnet test` — zero new failures (paste exact pass/fail count)
- [ ] `main` deployable: app starts, DI resolves, any new migration applies cleanly on a DB with existing rows
- [ ] ≤ 6 files changed/created (tests included)
- [ ] Human review before commit; PR raised using `.github/PULL_REQUEST_TEMPLATE.md`

### Whole change (after Phase 11)

- [ ] All `IFileClassificationRuleRepository` call sites removed
- [ ] Old entity / config / repository / domain files deleted; old table dropped
- [ ] Backfill verified: keyword count = token count; analyser L1 nodes seeded once; effective IsSpecial preserved
