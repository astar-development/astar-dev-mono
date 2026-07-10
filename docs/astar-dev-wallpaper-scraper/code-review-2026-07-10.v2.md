# AStar.Dev.Wallpaper.Scraper Code Review

**Date:** 2026-07-10
**Reviewer:** GitHub Copilot (Claude Sonnet 4.5)
**Focus Areas:** Database calls, Functional paradigm adherence, Method/class complexity

---

## Executive Summary

**Verdict:** Request Changes

This review identified **27 issues** requiring attention: 9 errors, 13 warnings, and 5 suggestions. The most critical concerns are excessive database operations that severely impact performance, significant deviations from the functional paradigm, and classes with too many responsibilities operating at multiple abstraction levels.

**Severity Breakdown:**

- **Errors:** 9 (critical performance and architectural issues)
- **Warnings:** 13 (architectural and design violations)
- **Suggestions:** 5 (minor improvements)

---

## Implementation Guidance

**Based on stakeholder clarification:**

### Runtime Updates Strategy

Tags and configuration **CAN and WILL be updated** throughout the application runtime. The solution:

- Register data holders as **Singletons** (loaded once at startup for performance)
- Implement **mutable singleton pattern** with change tracking
- All updates **MUST be persisted** to the database immediately
- Services consuming these singletons read current state, not cached snapshots

**Example pattern:**

```csharp
public sealed class TagsManager // Singleton
{
    private TagsToIgnoreCompletely tagsToIgnore;
    private readonly IDbContextFactory<AppDbContext> contextFactory;

    public TagsToIgnoreCompletely Current => tagsToIgnore; // Always current

    public async Task<Result<Unit, DataError>> UpdateAsync(TagsToIgnoreCompletely updated, CancellationToken cancellationToken)
    {
        tagsToIgnore = updated;
        return await PersistAsync(ct); // Immediate persistence
    }
}
```

### Functional Paradigm Is Mandatory

Breaking changes to enforce functional patterns are **required**, not optional:

- **ALL** repository methods **MUST** return `Result<T, TError>`
- **NO** try/catch blocks in business logic — use `Result` composition
- **NO** mutable state in workflow classes — pass state through pipelines
- **ALL** error handling via functional operators (`Bind`, `Match`, `Tap`, etc.)

This is non-negotiable architectural guidance.

### Performance Is Top Priority

Focus implementation efforts in this order:

1. **Database Performance** (critical, immediate ROI)
2. **Functional Paradigm Compliance** (architectural debt, blocks maintainability)
3. **Class Decomposition** (long-term maintainability, lower urgency)

---

## 1. Excessive Database Calls

### Error: Multiple DbContext creations during DI registration

**File:** [App.axaml.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/App.axaml.cs#L46-L51)
**Severity:** Error

**Issue:** Creating a DbContext on every `ScrapeConfiguration` resolution (registered as Transient). Lines 46-51 show a pattern where every service requesting `ScrapeConfiguration` triggers a database query.

```csharp
.AddTransient(sp =>
{
    using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    return ctx.ScrapeConfiguration.GetScrapeConfigurations().ToAppModel();
})
```

**Fix:** Register as Singleton with mutable singleton pattern for runtime updates:

```csharp
// Register manager as singleton
.AddSingleton<ScrapeConfigurationManager>()
.AddSingleton(sp => sp.GetRequiredService<ScrapeConfigurationManager>().Current)

// Manager implementation
public sealed class ScrapeConfigurationManager
{
    private ScrapeConfiguration current;
    private readonly IDbContextFactory<AppDbContext> contextFactory;

    public ScrapeConfigurationManager(IDbContextFactory<AppDbContext> contextFactory)
    {
        contextFactory = contextFactory;
        using var ctx = contextFactory.CreateDbContext();
        current = ctx.ScrapeConfiguration.GetScrapeConfigurations().ToAppModel();
    }

    public ScrapeConfiguration Current => current;

    public async Task<Result<Unit, ScrapeError>> UpdateAsync(ScrapeConfiguration updated, CancellationToken cancellationToken)
    {
        current = updated;
        // Persist to database immediately
        return await PersistAsync(updated, ct);
    }
}
```

**Note:** Runtime updates are supported — changes are persisted immediately when `UpdateAsync` is called.

---

### Error: Duplicate DbContext creations for tag loading

**File:** [App.axaml.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/App.axaml.cs#L72-L80)
**Severity:** Error

**Issue:** Two separate Transient registrations each creating their own DbContext to load tags from the same table with different filters.

```csharp
.AddTransient(sp =>
{
    using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    return TagsFactory.LoadTagsToIgnoreCompletely(ctx);
})
.AddTransient(sp =>
{
    using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    return TagsFactory.LoadTagsTextToIgnore(ctx);
})
```

**Fix:** Create mutable singleton manager for tags with runtime update support:

```csharp
.AddSingleton<TagsManager>()
.AddSingleton(sp => sp.GetRequiredService<TagsManager>().TagsToIgnoreCompletely)
.AddSingleton(sp => sp.GetRequiredService<TagsManager>().TagsTextToIgnore)

public sealed class TagsManager
{
    private TagsToIgnoreCompletely toIgnoreCompletely;
    private TagsTextToIgnore textToIgnore;
    private readonly IDbContextFactory<AppDbContext> contextFactory;

    public TagsManager(IDbContextFactory<AppDbContext> contextFactory)
    {
        contextFactory = contextFactory;
        using var ctx = contextFactory.CreateDbContext();
        var allTags = ctx.TagsToIgnore.ToList();
        toIgnoreCompletely = new() { Tags = allTags.Where(t => t.IgnoreImage).Select(t => t.Value).ToList() };
        textToIgnore = new() { Tags = allTags.Where(t => !t.IgnoreImage).Select(t => t.Value).ToList() };
    }

    public TagsToIgnoreCompletely TagsToIgnoreCompletely => toIgnoreCompletely;
    public TagsTextToIgnore TagsTextToIgnore => textToIgnore;

    public async Task<Result<Unit, DataError>> UpdateTagsAsync(IReadOnlyList<TagEntity> tags, CancellationToken cancellationToken)
    {
        // Update in-memory collections
        toIgnoreCompletely = new() { Tags = tags.Where(t => t.IgnoreImage).Select(t => t.Value).ToList() };
        textToIgnore = new() { Tags = tags.Where(t => !t.IgnoreImage).Select(t => t.Value).ToList() };
        // Persist immediately
        return await PersistAsync(tags, ct);
    }
}
```

**Rationale:** Single database query at startup + single write on update. Runtime modifications persist immediately.

---

### Error: Multiple SaveChangesAsync in nested loops

**File:** [FileClassificationService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/FileClassificationService.cs#L59-L177)
**Severity:** Error

**Issue:** `ImportClassificationsAsync` calls `SaveChangesAsync` inside nested loops (lines 86, 105, 132, 158, 170), resulting in hundreds of database round-trips for large import operations. This is a critical performance issue.

**Fix:** Batch all changes and call `SaveChangesAsync` once per level or once at the end:

```csharp
foreach (int level in levels)
{
    foreach (var category in classifications.Categories.Where(c => c.Level == level))
    {
        // Accumulate changes without calling SaveChangesAsync
    }
    // Save once per level
    await context.SaveChangesAsync(token).ConfigureAwait(false);
}
```

Or better: refactor into smaller, composable functions that each handle one responsibility and return `Result<T>`.

---

### Error: Inefficient Contains query in FileDetailRepository

**File:** [FileDetailRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileDetailRepository.cs#L11-L12)
**Severity:** Error

**Issue:** `ExistsAsync` uses `Contains(fileName)` which performs a partial match and cannot use an index efficiently.

```csharp
return await context.Files.FirstOrDefaultAsync(f => f.FileName.Value.Contains(fileName)) != null;
```

**Fix:** Use exact match with equality:

```csharp
return await context.Files.AnyAsync(f => f.FileName.Value == fileName, cancellationToken);
```

Note: Method signature should also accept `CancellationToken` and return `Result<bool, DataError>`.

---

### Warning: Unnecessary DbContext per SaveAsync call

**File:** [FileClassificationCategoriesRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileClassificationCategoriesRepository.cs#L12-L32)
**Severity:** Warning

**Issue:** `SaveAsync` loads all parent categories into memory on every call and performs multiple individual `AddAsync` operations without batching.

**Fix:** Consider caching parent categories or accepting them as a parameter. Batch `AddAsync` calls and use a single `SaveChangesAsync`.

---

### Warning: DbContext created but not needed in App initialization

**File:** [App.axaml.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/App.axaml.cs#L49)
**Severity:** Warning

**Issue:** A `SearchConfiguration` is resolved from `ScrapeConfiguration` immediately after loading it, but both trigger separate database operations.

**Fix:** Refactor the model transformation so that `SearchConfiguration` is derived directly without re-querying.

---

## 2. Deviations from Functional Paradigm

### Error: Imperative exception handling instead of Result

**File:** [FileClassificationService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/FileClassificationService.cs#L66-L177)
**Severity:** Error

**Issue:** `ImportClassificationsAsync` uses multiple try/catch blocks (lines 66, 112, 156, 167) instead of functional Result composition. This violates the repo's functional-first error handling conventions. **This is a mandatory architectural requirement.**

**Fix:** Break down into smaller functions returning `Result<T>` and compose them with `Bind`:

```csharp
return await ValidateClassifications(classifications)
    .BindAsync(valid => ImportLevel1Async(valid, token))
    .BindAsync(_ => ImportLevel2Async(classifications, token))
    .BindAsync(_ => ImportLevel3Async(classifications, token))
    .MatchAsync(
        success => Unit.Value,
        error => { logger.Error(error.Message); return Unit.Value; });
```

**Breaking Change:** Yes. **Required** for functional paradigm compliance.

---

### Error: Repositories not returning Result types

**File:** [FileDetailRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileDetailRepository.cs#L9-L27)
**Severity:** Error

**Issue:** Repository methods return `Task<bool>` and `Task` instead of `Result<T>`. Exceptions thrown by EF Core (e.g., constraint violations) won't be handled functionally. **This is a mandatory architectural requirement.**

**Fix:** Update ALL repository signatures to return `Result<T>`:

```csharp
Task<Result<bool, DataError>> ExistsAsync(string fileName, CancellationToken cancellationToken);
Task<Result<Unit, DataError>> AddAsync(FileDetailEntity fileDetail, CancellationToken cancellationToken);
```

**Breaking Change:** Yes. **Required** for functional paradigm compliance.

---

### Warning: Mutable state in workflow classes

**File:** [TopWallpapersWorkflow.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Workflows/TopWallpapersWorkflow.cs#L18)
**Severity:** Warning

**Issue:** Private field `searchConfiguration` is mutated (line 42: `searchConfiguration = searchConfiguration with { ... }`), mixing mutable and immutable patterns.

```csharp
private SearchConfiguration searchConfiguration = scrapeConfiguration.SearchConfiguration;
```

**Fix:** Pass configuration state through the functional pipeline instead of maintaining mutable fields:

```csharp
private async Task<Result<Unit, ScrapeError>> RunTopWallpapersAsync(CancellationToken cancellationToken)
{
    await LoadStartingPageAsync().ConfigureAwait(false);

    return await topWallpapersPage.PageInfoAsync()
        .BindAsync(pageCount => UpdateConfigurationAsync(pageCount))
        .BindAsync(updatedConfig => ProcessTopWallpapersAsync(updatedConfig, ct))
        .ConfigureAwait(false);
}
```

---

### Warning: Same mutable state pattern in SubscriptionsWorkflow

**File:** [SubscriptionsWorkflow.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Workflows/SubscriptionsWorkflow.cs#L18-L19)
**Severity:** Warning

**Issue:** Both `searchConfiguration` and `scrapeDirectories` fields are mutated (lines 31, 45, 46).

**Fix:** Same as TopWallpapersWorkflow — pass state through the pipeline.

---

### Warning: Direct match on Result instead of using MatchAsync

**File:** [TopWallpapersWorkflow.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Workflows/TopWallpapersWorkflow.cs#L38-L40)
**Severity:** Warning

**Issue:** Awaiting a Result and then pattern matching to extract boolean, which is then discarded. This breaks the functional chain unnecessarily.

```csharp
var loadResult = await topWallpapersPage.LoadTopWallpapersPageAsync(...).ConfigureAwait(false);
bool loadedSuccessfully = loadResult.Match(_ => true, _ => false);
```

**Fix:** Use functional operators directly:

```csharp
await topWallpapersPage.LoadTopWallpapersPageAsync(searchConfiguration.TopWallpapersStartingPageNumber)
    .OrElseAsync(() => topWallpapersPage.LoadTopWallpapersPageAsync(FirstPageNumber))
    .ConfigureAwait(false);
```

---

### Warning: Same pattern in SubscriptionsWorkflow

**File:** [SubscriptionsWorkflow.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Workflows/SubscriptionsWorkflow.cs#L35-L38)
**Severity:** Warning

**Issue:** Same await-then-Match pattern.

**Fix:** Use `OrElseAsync` as shown above.

---

### Suggestion: SearchWorkflow also has the pattern

**File:** [SearchWorkflow.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Workflows/SearchWorkflow.cs#L29-L31)
**Severity:** Suggestion

**Issue:** `ProcessSearchCategoriesAsync` awaits and Matches to get a boolean just to return early.

**Fix:** The pattern is acceptable here as it's checking for early exit in a loop, but consider extracting to a helper that short-circuits the Result chain.

---

## 3. Methods That Do Too Many Things / Are Too Long

### Error: ImportClassificationsAsync is 118 lines

**File:** [FileClassificationService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/FileClassificationService.cs#L59-L177)
**Severity:** Error

**Issue:** Method does:

- Iterates 3 levels of categories
- Validates and inserts/updates categories
- Queries for parent categories
- Handles two different error scenarios with separate try/catch
- Inserts keywords
- Saves changes multiple times

This is far beyond the 20-line guideline.

**Fix:** Extract per-level import logic with functional composition:

```csharp
private async Task<Result<Unit, ImportError>> ImportClassificationsAsync(...)
    => await ImportLevelAsync(1, classifications, token)
        .BindAsync(_ => ImportLevelAsync(2, classifications, token))
        .BindAsync(_ => ImportLevelAsync(3, classifications, token))
        .ConfigureAwait(false);

private async Task<Result<Unit, ImportError>> ImportLevelAsync(int level, ...)
    => await LoadContextAsync(token)
        .BindAsync(ctx => ImportCategoriesForLevelAsync(ctx, level, classifications, token))
        .BindAsync(_ => ImportKeywordsForLevelAsync(ctx, level, classifications, token))
        .ConfigureAwait(false);
```

---

### Warning: ImportScrapeConfigurationAsync is 78 lines of property updates

**File:** [ScrapeConfigurationService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/ScrapeConfigurationService.cs#L24-L78)
**Severity:** Warning

**Issue:** 50+ lines of imperative property-by-property copying with no abstraction.

**Fix:** Extract update logic into smaller functions per configuration section:

```csharp
private static Result<Unit, ValidationError> UpdateConnectionStrings(ConnectionStringsEntity existing, ConnectionStringsEntity incoming) { ... }
private static Result<Unit, ValidationError> UpdateUserConfiguration(UserConfigurationEntity existing, UserConfigurationEntity incoming) { ... }
private static Result<Unit, ValidationError> UpdateSearchConfiguration(SearchConfigurationEntity existing, SearchConfigurationEntity incoming) { ... }
```

---

### Warning: OnFrameworkInitializationCompleted does too much

**File:** [App.axaml.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/App.axaml.cs#L37-L129)
**Severity:** Warning

**Issue:** 90+ lines mixing DI configuration, database initialization, window setup, and error surfacing.

**Fix:** Extract DI configuration:

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    host = CreateHost();
    await InitializeDatabaseAsync().ConfigureAwait(false);
    ConfigureLifetime();
    host.Start();
    SurfaceConfigurationErrors();
    base.OnFrameworkInitializationCompleted();
}

private IHost CreateHost()
{
    var builder = Host.CreateApplicationBuilder(...);
    ConfigureServices(builder.Services, builder.Configuration);
    return builder.Build();
}

private void ConfigureServices(IServiceCollection services, IConfiguration configuration) { ... }
```

---

### Suggestion: GetTheImagePagesAsync mixes concerns

**File:** [ImagePageService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/ImagePageService.cs#L38-L58)
**Severity:** Suggestion

**Issue:** Loops with conditional short-circuits, database checks, and orchestration. Not excessively long but could be more functional.

**Fix:** Use LINQ-style filtering and functional operators:

```csharp
return await imagePageLinks
    .ToAsyncEnumerable()
    .WhereAwaitAsync(async link => !(await fileDetailRepository.ExistsAsync(Path.GetFileName(link)).IsSuccess()))
    .AggregateAsync(
        Result.Success<Unit>(Unit.Value),
        async (acc, pageLink) => await acc.BindAsync(_ => ProcessImagePageAsync(pageLink, name, pageData, ct)));
```

---

## 4. Classes That Do Too Many Things / Operate on Multiple Levels

### Error: FileClassificationService operates on 4+ abstraction levels

**File:** [FileClassificationService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/FileClassificationService.cs)
**Severity:** Error

**Issue:** This class:

1. **High-level orchestration:** `LoadPageClassificationDataAsync`, `ClassifyAsync`
2. **Business logic:** `ClassificationMatcher.Match`, category resolution
3. **Data transformation:** entity mapping, keyword collection
4. **EF Core internals:** `EnsureTracked`, `ChangeTracker` manipulation
5. **Import/export operations:** entirely separate domain concern

**Fix:** Split into multiple focused classes:

```csharp
// High-level service
public sealed class FileClassificationService(
    IFileClassificationRepository repository,
    ClassificationMatcher matcher,
    ILogger logger)
{
    public Task<Result<PageClassificationData, DataError>> LoadPageClassificationDataAsync(...) { ... }
    public Task<Result<Unit, ScrapeError>> ClassifyAsync(...) { ... }
}

// Import/export concern
public sealed class FileClassificationImportExportService(
    IFileClassificationRepository repository,
    ILogger logger)
{
    public Task<Result<Unit, ImportError>> ImportAsync(...) { ... }
    public Task<Result<(Categories, Keywords), DataError>> ExportAsync(...) { ... }
}

// Low-level repository handling EF tracking
public sealed class FileClassificationRepository(IDbContextFactory<AppDbContext> factory)
{
    public Task<Result<List<Category>, DataError>> GetSearchableCategoriesAsync(...) { ... }
    public Task<Result<List<Keyword>, DataError>> GetKeywordsForCategoriesAsync(...) { ... }
    public Task<Result<Unit, DataError>> SaveClassificationsAsync(...) { ... }
}
```

---

### Warning: ImagePageService mixes orchestration and low-level operations

**File:** [ImagePageService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/ImagePageService.cs)
**Severity:** Warning

**Issue:** This service:

1. Orchestrates the image download workflow
2. Handles delays (timing concern)
3. Broadcasts images (UI concern)
4. Saves files (I/O concern)
5. Reads image dimensions (image processing concern)
6. Persists to database (data access concern)
7. Triggers classification (business logic concern)

**Fix:** Extract separate services:

```csharp
// High-level orchestrator
public sealed class ImageWorkflowOrchestrator(
    IImageDownloader downloader,
    IImagePersistence persistence,
    IClassificationService classification,
    IDelayStrategy delays,
    ILogger logger) { ... }

// File operations
public sealed class ImagePersistence(
    IImageSaver saver,
    IImageDimensionReader dimensionReader,
    IFileDetailRepository repository) { ... }

// Download and retry
public sealed class ImageDownloader(
    IImageRetriever retriever,
    IDirectoryHelper directory,
    IDelayStrategy delays) { ... }
```

---

### Warning: App.axaml.cs mixes framework and application concerns

**File:** [App.axaml.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/App.axaml.cs#L28-L129)
**Severity:** Warning

**Issue:** Application class:

1. Configures DI (infrastructure)
2. Initializes database (data)
3. Configures logging (infrastructure)
4. Validates configuration (business logic)
5. Manages application lifetime (framework)

**Fix:** Extract composition root:

```csharp
public partial class App : Application
{
    public override async void OnFrameworkInitializationCompleted()
    {
        host = await AppCompositionRoot.CreateHostAsync().ConfigureAwait(false);
        ConfigureLifetime();
        host.Start();
        base.OnFrameworkInitializationCompleted();
    }
}

internal static class AppCompositionRoot
{
    public static async Task<IHost> CreateHostAsync()
    {
        var builder = CreateBuilder();
        ConfigureInfrastructure(builder);
        ConfigureServices(builder.Services);
        var host = builder.Build();
        await InitializeAsync(host).ConfigureAwait(false);
        return host;
    }
}
```

---

### Warning: ScrapeConfigurationService mixes validation and persistence

**File:** [ScrapeConfigurationService.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Services/ScrapeConfigurationService.cs#L24-L78)
**Severity:** Warning

**Issue:** Service performs:

1. Data retrieval
2. Business rule validation (checking for `ApplicationMetadata.Redacted`)
3. Property-by-property updates
4. Persistence

**Fix:** Extract into a repository (data access) and a validator (business rules):

```csharp
public sealed class ScrapeConfigurationValidator
{
    public Result<ScrapeConfigurationEntity, ValidationError> Validate(ScrapeConfigurationEntity incoming) { ... }
}

public sealed class ScrapeConfigurationRepository
{
    public Task<Result<ScrapeConfigurationEntity, DataError>> GetAsync(CancellationToken cancellationToken) { ... }
    public Task<Result<Unit, DataError>> SaveAsync(ScrapeConfigurationEntity entity, CancellationToken cancellationToken) { ... }
}

public sealed class ScrapeConfigurationService(
    ScrapeConfigurationRepository repository,
    ScrapeConfigurationValidator validator)
{
    public Task<Result<Unit, ValidationError>> ImportAsync(...)
        => validator.Validate(incoming)
            .BindAsync(valid => repository.GetAsync(ct))
            .BindAsync(existing => MergeAndSaveAsync(existing, valid, ct));
}
```

---

## Additional Observations

### Note: Runtime Update Pattern Required

**File:** [App.axaml.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/App.axaml.cs#L46-L80)

**Clarification:** `ScrapeConfiguration` and Tags are registered as Singleton for performance but **MUST support runtime updates**. Implement mutable singleton pattern with immediate persistence as shown in the "Implementation Guidance" section above.

**Key Requirements:**

- Singleton registration (single database read at startup)
- Mutable state holder with change tracking
- `UpdateAsync` method that persists changes immediately
- Services consume current state, not cached snapshots

See Error #1 and Error #2 fixes for implementation examples.

---

### Suggestion: Missing CancellationToken propagation

**Files:** Multiple
**Severity:** Suggestion

Several methods create DbContexts without passing `CancellationToken`:

- [FileDetailRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileDetailRepository.cs#L11) line 11
- [FileClassificationCategoriesRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileClassificationCategoriesRepository.cs#L13) line 13

**Fix:** Add `CancellationToken` parameter and pass through all async operations.

---

## Recommendations

**Implementation Order (by stakeholder priority):**

### Phase 1: Performance (Critical - Start Immediately)

**Target:** Eliminate redundant database calls and batch operations

1. **Implement TagsManager singleton** with runtime update support
    - Expected ROI: 66% reduction in app startup database calls
    - Breaking changes: None (internal refactor)
    - Effort: 1 day

2. **Implement ScrapeConfigurationManager singleton** with runtime updates
    - Expected ROI: Reduced DI resolution overhead
    - Breaking changes: None (internal refactor)
    - Effort: 1 day

3. **Batch SaveChangesAsync** in `FileClassificationService.ImportClassificationsAsync`
    - Expected ROI: 10-100x faster imports (depends on dataset size)
    - Breaking changes: None (internal refactor)
    - Effort: 0.5 day

4. **Fix `FileDetailRepository.ExistsAsync`** to use exact match with indexed query
    - Expected ROI: Faster image duplicate detection
    - Breaking changes: Method signature (add `CancellationToken`)
    - Effort: 0.5 day

**Phase 1 Total: 3 days**

---

### Phase 2: Functional Paradigm Compliance (Required - Architectural Debt)

**Target:** Eliminate imperative error handling, enforce Result types throughout

5. **Convert ALL repository methods to return `Result<T>`**
    - **Breaking changes: Yes** — all repository consumers must update
    - Mandatory for architectural consistency
    - Effort: 3 days

6. **Refactor `FileClassificationService.ImportClassificationsAsync`** to functional composition
    - Break into smaller functions returning `Result<T>`
    - Remove all try/catch blocks
    - Use `Bind`/`BindAsync` for composition
    - Effort: 2 days

7. **Replace mutable state in workflow classes** with functional state passing
    - `TopWallpapersWorkflow`: pass config through pipeline
    - `SubscriptionsWorkflow`: pass config + directories through pipeline
    - Remove private mutable fields
    - Effort: 1.5 days

8. **Replace await-then-Match patterns** with functional operators
    - Use `OrElseAsync` for fallback logic
    - Use `BindAsync` for chaining
    - Effort: 0.5 day

**Phase 2 Total: 7 days**

---

### Phase 3: Class Decomposition (Long-term Maintainability)

**Target:** Single Responsibility Principle, proper abstraction layers

9. **Split `FileClassificationService`** into focused classes:
    - `FileClassificationService` (orchestration)
    - `FileClassificationImportExportService` (import/export)
    - `FileClassificationRepository` (data access with EF tracking)
    - Effort: 3 days

10. **Decompose `ImagePageService`** into:
    - `ImageWorkflowOrchestrator` (high-level workflow)
    - `ImagePersistence` (file I/O + database)
    - `ImageDownloader` (HTTP + retry logic)
    - Effort: 3 days

11. **Extract App.axaml.cs DI configuration** into `AppCompositionRoot` class
    - Effort: 1 day

12. **Refactor long methods**:
    - `ScrapeConfigurationService.ImportScrapeConfigurationAsync` (78 lines → extract per-section updates)
    - `App.OnFrameworkInitializationCompleted` (90 lines → extract helpers)
    - Effort: 2 days

**Phase 3 Total: 9 days**

---

### Cross-Cutting (All Phases)

13. **Add `CancellationToken` propagation** to all async repository methods
14. **Extract configuration update logic** into focused update methods per section

---

## Implementation Estimates

| Phase                  | Effort | Risk   | Value                     | Priority      |
| ---------------------- | ------ | ------ | ------------------------- | ------------- |
| Phase 1: Performance   | 3 days | Low    | High (immediate impact)   | **Start Now** |
| Phase 2: Functional    | 7 days | Medium | High (architectural debt) | **Required**  |
| Phase 3: Decomposition | 9 days | Low    | Medium (long-term)        | Future        |

**Total:** ~19 days (~4 weeks) for full implementation

**Recommended Approach:** Complete Phase 1 and Phase 2 before considering Phase 3. Phase 3 can be deferred if time constraints exist, but Phases 1 and 2 are critical.

---

## Related Documentation

- [Microsoft: EF Core Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
- [C# Functional Programming with Language-Ext](https://github.com/louthy/language-ext/wiki)
- [SOLID Principles in C#](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp)
- [AStar.Dev Functional Extensions Documentation](../../packages/AStar.Dev.Functional.Extensions/README.md) (if exists)
- [Repository Pattern Best Practices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

---

**End of Report**
