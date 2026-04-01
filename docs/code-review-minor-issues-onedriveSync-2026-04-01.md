# OneDriveSync Code Review — Minor Issues (2026-04-01)

Blockers and Major issues were fixed in PR #108. The following minor issues are tracked here for future cleanup.

| # | File | Issue |
|---|------|-------|
| m-1 | `Features/Accounts/Account.cs:14` | `Id` is a raw `Guid` — should be strongly-typed via `AStar.Dev.Source.Generators.Attributes` |
| m-2 | `Features/Accounts/SyncedFileMetadata.cs:13` | `Id` (`long`) and `AccountId` (`Guid`) should be strongly-typed; `RelativePath`/`FileName` should use an `IFileSystem`-compatible abstraction rather than `string` |
| m-3 | `Features/Accounts/Account.cs:26` | `AuthState` stored as raw `string` with hardcoded default `"Authenticated"` — should be an `enum` |
| m-4 | `Infrastructure/Persistence/AppSettings.cs:24` | `UserType` stored as `string` with hardcoded default `"Casual"` — should reference the `UserType` enum directly |
| m-5 | `Infrastructure/Persistence/PersistenceServiceExtensions.cs:33` | Connection string built via `$"DataSource={dbPath}"` — use `SqliteConnectionStringBuilder` to prevent special-character injection |
| m-6 | `Infrastructure/Localisation/LocalisationService.cs:40` | Complex two-level pattern-match destructure on one line — extract to a named helper variable for readability |
| m-7 | `Infrastructure/Shell/FeatureAvailabilityService.cs:19` | Missing space after `if(` — `if(_frozenSections` should be `if (_frozenSections` |
| m-8 | `Tests.Unit/Shell/GivenAFeatureAvailabilityService.cs:39` | Typo in test name: `availabl` → `available` |
| m-9 | `Tests.Unit/AStar.Dev.OneDriveSync.Tests.Unit.csproj:5-8` | Comment block restates the project name and `OutputType` — redundant per no-redundant-comments rule |
| m-10 | `Infrastructure/Startup/StartupOrchestrator.cs:9` | `tasks.ToList()` materialized solely for `.Count` — use `.Count()` inline or inject `IReadOnlyList<IStartupTask>` |
| m-11 | `Infrastructure/ViewLocator.cs:28` | `"Not Found: " + name` — use interpolation; this hardcoded English string also bypasses `ILocalisationService` |
| m-12 | `App.axaml.cs:27` | `?.ToString() ?? "unknown"` in `_appVersion` — the fallback string could be a named constant |
| m-13 | `Infrastructure/Theming/AppSettingsRepository.cs:41-43` | `if/else` for Add vs Update — an early-return guard for the Add path would improve scan-ability |
| m-14 | `Tests.Unit/AStar.Dev.OneDriveSync.Tests.Unit.csproj` | `<IsPublishable>false</IsPublishable>` is set automatically for test projects by `Directory.Build.props` — declaration is redundant |
