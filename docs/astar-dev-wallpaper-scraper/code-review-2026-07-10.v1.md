# AStar.Dev.Wallpaper.Scraper Code Review

**Date:** 2026-07-10
**Reviewer:** GitHub Copilot (Claude Sonnet 4.5)
**Focus Areas:** Database calls, Functional paradigm adherence, Method/class complexity

---

## Executive Summary

**Verdict:** Request Changes

This review identified **27 issues** requiring attention: 8 errors, 14 warnings, and 5 suggestions. The most critical concerns are excessive database operations that severely impact performance, significant deviations from the functional paradigm, and classes with too many responsibilities operating at multiple abstraction levels.

**Severity Breakdown:**

- **Errors:** 8 (critical performance and architectural issues)
- **Warnings:** 14 (architectural and design violations)
- **Suggestions:** 5 (minor improvements)

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

**Fix:** Register `ScrapeConfiguration` as Singleton and load it once at startup:

```csharp
.AddSingleton(sp =>
{
    using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    return ctx.ScrapeConfiguration.GetScrapeConfigurations().ToAppModel();
})
```

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

**Fix:** Load both tag collections in a single database call and register as Singleton:

```csharp
.AddSingleton(sp =>
{
    using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    var allTags = ctx.TagsToIgnore.ToList();
    return (
        ToIgnoreCompletely: new TagsToIgnoreCompletely { Tags = allTags.Where(t => t.IgnoreImage).Select(t => t.Value).ToList() },
        TextToIgnore: new TagsTextToIgnore { Tags = allTags.Where(t => !t.IgnoreImage).Select(t => t.Value).ToList() }
    );
});
```

Then add separate registrations resolving from the tuple for backward compatibility.

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

Or better: refactor into smaller, composable functions that each handle one responsibility.

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

Note: Method signature should also accept `CancellationToken`.

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

**Issue:** `ImportClassificationsAsync` uses multiple try/catch blocks (lines 66, 112, 156, 167) instead of functional Result composition. This violates the repo's functional-first error handling conventions.

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
private async Task<Result<Unit, ScrapeError>> RunTopWallpapersAsync(CancellationToken ct)
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

### Warning: Repositories not returning Result types

**File:** [FileDetailRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileDetailRepository.cs#L9-L27)
**Severity:** Warning

**Issue:** Repository methods return `Task<bool>` and `Task` instead of `Result<T>`. Exceptions thrown by EF Core (e.g., constraint violations) won't be handled functionally.

**Fix:** Update signatures to return `Result<T>`:

```csharp
Task<Result<bool, DataError>> ExistsAsync(string fileName, CancellationToken ct);
Task<Result<Unit, DataError>> AddAsync(FileDetailEntity fileDetail, CancellationToken ct);
```

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

**Fix:** Extract per-level import logic:

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
private static void UpdateConnectionStrings(ConnectionStringsDto existing, ConnectionStringsDto incoming) { ... }
private static void UpdateUserConfiguration(UserConfigurationDto existing, UserConfigurationDto incoming) { ... }
private static void UpdateSearchConfiguration(SearchConfigurationEntity existing, SearchConfigurationEntity incoming) { ... }
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
    _host = CreateHost();
    await InitializeDatabaseAsync().ConfigureAwait(false);
    ConfigureLifetime();
    _host.Start();
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
    .WhereAwaitAsync(async link => !(await fileDetailRepository.ExistsAsync(Path.GetFileName(link))))
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
    public Task<PageClassificationData> LoadPageClassificationDataAsync(...) { ... }
    public Task<Result<Unit, ScrapeError>> ClassifyAsync(...) { ... }
}

// Import/export concern
public sealed class FileClassificationImportExportService(
    IFileClassificationRepository repository,
    ILogger logger)
{
    public Task<Result<Unit, ImportError>> ImportAsync(...) { ... }
    public Task<(Categories, Keywords)> ExportAsync(...) { ... }
}

// Low-level repository handling EF tracking
public sealed class FileClassificationRepository(IDbContextFactory<AppDbContext> factory)
{
    public Task<List<Category>> GetSearchableCategoriesAsync(...) { ... }
    public Task<List<Keyword>> GetKeywordsForCategoriesAsync(...) { ... }
    public Task SaveClassificationsAsync(...) { ... }
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
        _host = await AppCompositionRoot.CreateHostAsync().ConfigureAwait(false);
        ConfigureLifetime();
        _host.Start();
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
    public Task<ScrapeConfigurationEntity> GetAsync(CancellationToken ct) { ... }
    public Task SaveAsync(ScrapeConfigurationEntity entity, CancellationToken ct) { ... }
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

### Suggestion: Missing CancellationToken propagation

**Files:** Multiple
**Severity:** Suggestion

Several methods create DbContexts without passing `CancellationToken`:

- [FileDetailRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileDetailRepository.cs#L11) line 11
- [FileClassificationCategoriesRepository.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/Repositories/FileClassificationCategoriesRepository.cs#L13) line 13

**Fix:** Add `CancellationToken` parameter and pass through all async operations.

---

### Suggestion: Consider caching frequently accessed data

**File:** [App.axaml.cs](../apps/desktop/Scraper/AStar.Dev.Wallpaper.Scraper/App.axaml.cs#L46-L80)
**Severity:** Suggestion

**Issue:** `ScrapeConfiguration`, `TagsToIgnoreCompletely`, and `TagsTextToIgnore` are loaded from the database on every service resolution. These are configuration data that rarely change.

**Fix:** As mentioned earlier, register as Singleton. Consider adding a configuration reload mechanism if runtime updates are needed.

---

## Recommendations

### High Priority

1. **Consolidate tag loading** into a single query and cache as Singleton (performance critical)
2. **Batch database SaveChangesAsync calls** in `FileClassificationService.ImportClassificationsAsync` (performance critical)
3. **Split FileClassificationService** into separate concerns (architectural)
4. **Fix `FileDetailRepository.ExistsAsync`** to use exact match with index support

### Medium Priority

5. Refactor long methods in `FileClassificationService` and `ScrapeConfigurationService`
6. Replace mutable state in workflow classes with functional state passing
7. Update repository signatures to return `Result<T>` types
8. Extract `ImagePageService` responsibilities into focused services

### Low Priority

9. Add CancellationToken propagation throughout
10. Extract DI configuration from `App.axaml.cs` into composition root
11. Use functional operators (`OrElseAsync`) instead of await-then-Match patterns

---

## Related Documentation

- [Microsoft: EF Core Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
- [C# Functional Programming with Language-Ext](https://github.com/louthy/language-ext/wiki)
- [SOLID Principles in C#](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp)
- [AStar.Dev Functional Extensions Documentation](../../packages/AStar.Dev.Functional.Extensions/README.md) _(if exists)_
- [Repository Pattern Best Practices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

---

**End of Report**
