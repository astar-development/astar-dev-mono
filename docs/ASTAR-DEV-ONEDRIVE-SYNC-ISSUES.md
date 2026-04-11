 ---
  🚨 Errors (13)

  ┌─────┬───────────────────────────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────────────────────┐
  │  #  │               File                │                                                         Issue                                                         │                                     Fix                                      │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ ✅1   │ AuthService.cs:19                 │ Azure client ID hardcoded in source                                                                                   │ Read exclusively from OneDriveClientOptions; never a literal                 │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ ✅2   │ App.axaml.cs:83                   │ Same client ID duplicated as env-var fallback                                                                         │ Throw if env var absent; no fallback literal                                 │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ ✅3   │ SyncScheduler.cs:14               │ _running read/written from timer thread-pool + calling thread with no sync — data race                                │ private volatile bool _running; or Interlocked.CompareExchange               │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 4   │ GraphService.cs:13                │ _cache keyed on raw access token — unbounded memory leak                                                              │ Key on stable account ID                                                     │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 5   │ GraphService.cs:239               │ ResolveClientWithDriveContextAsync is not thread-safe — concurrent callers both miss and both write                   │ SemaphoreSlim or ConcurrentDictionary with async factory                     │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ ✅6   │ AccountsViewModel.cs:99           │ Debug.WriteLine with PII (email + account ID) in production code                                                      │ Remove it                                                                    │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 7   │ SyncServiceTests.cs:9             │ No Given prefix, no when_..._then_... names                                                    │ Rename to GivenASyncService, methods to snake_case                           │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 8   │ SyncServiceTests.cs:222           │ Test has no assertion — comment says "should attempt" but verifies nothing                                            │ Add Received() calls or remove the test                                      │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 9   │ SyncSchedulerTests.cs:9           │ Same naming violations as #7                                                                                          │ Same fix                                                                     │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 10  │ SyncSchedulerTests.cs:26–85       │ Multiple tests assert only ShouldNotBeNull() — verify nothing meaningful                                              │ Assert observable behaviour (events fired, state changed)                    │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 11  │ SyncSchedulerTests.cs:186         │ Test expects ThrowAsync but TriggerAccountAsync has no try/catch — expectation is wrong given current production code │ Align test to actual behaviour or fix production resilience gap              │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 12  │ SyncServiceTests.cs (all methods) │ Every test re-declares all 6 mocks locally — massive duplication                                                      │ Promote mocks to class-level fields with beforeEach-style constructor setup  │
  ├─────┼───────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 13  │ SettingsService.cs:19             │ Static LoadAsync() factory + Transient DI registration = callers get an unloaded instance                             │ Remove Transient registration; initialise via IHostedService or startup task │
  └─────┴───────────────────────────────────┴───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────────────────────┘

  ---
  ⚠️  Warnings (26)

  ┌─────┬────────────────────────────┬──────────────────────────────────────────────────────────────────────────────────────────┐
  │  #  │            File            │                                          Issue                                           │
  ├─────┼─────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅14  │ Both .csproj files          │ <TargetFramework> and <ImplicitUsings> redeclared — inherited from Directory.Build.props │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅15  │ AuthService.cs:20           │ Typo: AuthorityForMicrosoftAccountsOunly → AuthorityForMicrosoftAccountsOnly                        │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅16  │ SyncService.cs:279          │ #pragma warning disable CA2208 — use nameof(outcome) instead                                        │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅17  │ LocalChangeDetector.cs:21   │ #pragma warning disable CA1822 with "not sure why" comment — document the reason                    │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 18  │ SyncScheduler.cs:78         │ CancellationToken.None passed to RunSyncPassAsync — shutdown can hang                               │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅19  │ SyncService.cs:149          │ Typo: ProcessPownloadDeltasAsync → ProcessDownloadDeltasAsync                                       │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅20  │ SyncService.cs:288          │ ParallelDownloadPipeline newed up directly — violates DI, untestable                                │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅21  │ LocalChangeDetector.cs:49   │ const with inline comment inside method — remove comment; rename constant if needed                 │
  ├─────┼─────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅22  │ LocalChangeDetector.cs:110  │ IsFileToSkip is a 200+ char one-liner compound predicate — extract named helpers                    │
  ├─────┼────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 23  │ UploadService.cs:127           │ chunk.ToArray() allocates on every chunk PUT — use ReadOnlyMemoryContent or equivalent              │
  ├─────┼────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅24  │ AccountsViewModel.cs:76        │ async void event handler — wrap body in try/catch to prevent silent crash                           │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 25  │ AccountsViewModel.cs:123         │ Task discarded with _ — exceptions silently swallowed                                               │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ ✅26  │ AccountCardViewModel.cs:107      │ AccentPalette and Palette are identical arrays                                                          │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 27  │ OneDriveAccount.cs:20            │ List<string> / Dictionary<string,string> exposed directly — use IReadOnlyList / IReadOnlyDictionary     │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 28  │ OneDriveAccount.cs               │ Mutable sealed class acting as DTO — should be a record with factory class                              │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 29  │ SyncJob.cs:16                    │ State, ErrorMessage, CompletedAt use set on an otherwise init-only record                               │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 30  │ MainWindowViewModel.cs:26        │ Primary constructor has 10 parameters — max ~5; introduce sub-service groupings                         │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 31  │ MainWindowViewModel.cs:115       │ Accounts, Files, Activity etc. newed up in class body — inject via DI                                   │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 32  │ MainWindowViewModel.cs:51        │ return not preceded by blank line in switch expression arms                                             │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 33  │ MainWindowViewModel.cs:205       │ Exception double-serialised in .TapErrorAsync — e passed as both exception and {Error} property         │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 34  │ SplashWindowViewModel.cs:9       │ Old-style [ObservableProperty] private string _status — use C#14 partial property syntax                │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 35  │ StartupServiceExtensions.cs:11   │ IFileSystem registered twice — second silently overwrites first                                         │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 36  │ SyncRepository.cs:9              │ IEnumerable<SyncJob> may be enumerated twice — materialise with .ToList()                               │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 37  │ AddAccountWizardViewModel.cs:174 │ await Task.CompletedTask is dead code; Cancelled only fires when mid-auth                               │
  ├─────┼──────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ 38  │ Throughout codebase              │ Serilog.Log.* static logger used everywhere — inject ILogger<T> and use LogMessage compile-time helpers │
  └─────┴──────────────────────────────────┴─────────────────────────────────────────────────────────────────────────────────────────────────────────┘

  ---
  💡 Suggestions (4)

  ┌─────┬──────────────────────────────────┬──────────────────────────────────────────────────────────────────────────────┐
  │  #  │               File               │                                    Issue                                     │
  ├─────┼──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 39  │ ActivityViewModel.cs:15          │ Typo: MaxLogSixe → MaxLogSize                                                │
  ├─────┼──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 40  │ AccountEntity.cs:17              │ Inline // Sync settings comment inside class body — remove                   │
  ├─────┼──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 41  │ ViewModelBase.cs                 │ Empty abstract class adds no value over inheriting ObservableObject directly │
  ├─────┼──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────┤
  │ 42  │ AddAccountWizardViewModel.cs:149 │ x != null ? x : string.Empty → x ?? string.Empty                             │
  └─────┴──────────────────────────────────┴──────────────────────────────────────────────────────────────────────────────┘

  ---
  🚧 Incomplete Features

  ┌──────────────────────────────────┬──────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────────────────────────────────────────────────┐
  │             Feature              │                     Location                     │                                                  Status                                                  │
  ├──────────────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ Folder picker                    │ AccountSyncSettingsViewModel.BrowseAsync         │ Empty method — "wired via code-behind" per comment but no enforcement                                    │
  ├──────────────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ Files nav section                │ ShellServiceExtensions.RegisterAvailableFeatures │ NavSection.Files absent from registration; navigation targets it but it can't be registered as a feature │
  ├──────────────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ Resume-from-queue                │ SyncRepository.GetPendingJobsAsync               │ Called by interface; never used in any sync code path — dead code or unimplemented path                  │
  ├──────────────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────┤
  │ Localisation / culture switching │ SetCultureAsync                                  │ Culture reloads strings but does not propagate reactively to bound UI — switching is incomplete          │
  └──────────────────────────────────┴──────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────────────────────────────────────────────────┘

  ---
  Verdict

  ┌───────────────┬───────┐
  │   Severity    │ Count │
  ├───────────────┼───────┤
  │ 🔴 error      │ 13    │
  ├───────────────┼───────┤
  │ 🟡 warning    │ 26    │
  ├───────────────┼───────┤
  │ 🔵 suggestion │ 4     │
  └───────────────┴───────┘

  🔴 Request changes.

  Hard blockers: hardcoded Azure client ID (#1, #2), SyncScheduler data race (#3), GraphService memory leak (#4, #5). Fix those before anything else ships.

✻ Sautéed for 5m 22s
