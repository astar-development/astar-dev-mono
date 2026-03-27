# S002 — Database Foundation: Code Review Issues

**Reviewed against:** `docs/OneDriveSync/S002-Database-Foundation.md`, `docs/ONEDRIVE-SPEC.md`
**Commits in scope:** `05d85c1`, `943fab5`
**Review date:** 2026-03-27

---

## Issues

### ERROR-01 — `MigrateAsync()` is never called; no DI container is wired up

**File:** `apps/desktop/AStar.Dev.OneDriveSync/App.axaml.cs`, `apps/desktop/AStar.Dev.OneDriveSync/Program.cs`
**Severity:** error

**Issue:** S002 AC "Schema & Migrations" requires `Database.MigrateAsync()` to be called at startup and `EnsureCreatedAsync` to never be used. Neither `Program.cs` nor `App.axaml.cs` configures a DI container, registers `AppDbContext`, or performs any startup migration. `OnFrameworkInitializationCompleted` directly `new`s up `MainWindowViewModel()` with no service resolution. The entire persistence layer is disconnected from the running application.

S002 Technical Notes also require migration success/failure to be logged at `Information`/`Error` respectively (NF-00) — there is no place for this to happen.

**Fix (skeleton):**

```csharp
// App.axaml.cs — OnFrameworkInitializationCompleted
private IServiceProvider? _services;

public override async void OnFrameworkInitializationCompleted()
{
    _services = BuildServiceProvider();

    var db = _services.GetRequiredService<AppDbContext>();
    var logger = _services.GetRequiredService<ILogger<App>>();

    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed — routing to recovery flow");
        // Route to S003 corrupt-DB recovery screen
    }

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        DisableAvaloniaDataAnnotationValidation();
        desktop.MainWindow = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainWindowViewModel>(),
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

---

### ERROR-02 — No logging anywhere in the persistence layer (NF-00 violation)

**Files:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/DbBackupService.cs`,
`apps/desktop/AStar.Dev.OneDriveSync/AStar.Dev.OneDriveSync.csproj`
**Severity:** error

**Issue:** `NF-00` states _"Logging is NOT optional. All stories / features MUST include suitable logging."_ The S002 spec's Technical Notes explicitly require migration success/failure to be logged at `Information`/`Error`. Neither `DbBackupService` nor any other production class in this story injects or calls `ILogger`. Additionally, no logging framework (Serilog, `Microsoft.Extensions.Logging`) appears in the `.csproj` `PackageReference` list, so there is no logging infrastructure at all.

**Fix — add `ILogger<DbBackupService>` to `DbBackupService`:**

```csharp
public sealed class DbBackupService(
    IAppDataPathProvider pathProvider,
    ILogger<DbBackupService> logger) : IDbBackupService
{
    public async Task<Result<bool, ErrorResponse>> BackupAsync()
    {
        var dataFilePath = Path.Combine(pathProvider.AppDataDirectory, DatabaseFileName);

        if (!File.Exists(dataFilePath))
        {
            logger.LogError("Backup failed: database file not found at '{Path}'", dataFilePath);

            return new Result<bool, ErrorResponse>.Error(
                new ErrorResponse($"Database file not found at '{dataFilePath}'. Cannot create backup."));
        }

        var backupFilePath = Path.Combine(pathProvider.AppDataDirectory, BackupFileName);
        await using var source      = File.OpenRead(dataFilePath);
        await using var destination = File.Open(backupFilePath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(destination).ConfigureAwait(false);

        logger.LogInformation("Database backed up to '{BackupPath}'", backupFilePath);

        return new Result<bool, ErrorResponse>.Ok(true);
    }
}
```

Also add a Serilog or `Microsoft.Extensions.Logging` package reference to the `.csproj`.

---

### ERROR-03 — `DateTimeOffsetToUnixMillisecondsConverter` is dead code; no global registration

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/Converters/DateTimeOffsetToUnixMillisecondsConverter.cs`,
`apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/AppDbContext.cs`
**Severity:** error

**Issue:** S002 AC DB-01 requires all `DateTimeOffset` properties to be stored as Unix milliseconds via a value converter, implemented once and reused. The converter class exists but is never registered. `AppDbContext` does not override `ConfigureConventions`, so EF Core never applies the converter to any property. Per the "reused — not duplicated per entity" requirement, the correct approach is a single global convention registration, not manual `HasConversion` calls in each `IEntityTypeConfiguration`. Without this, any future entity with a `DateTimeOffset` property will silently store it as a text column, violating DB-01.

**Fix — add to `AppDbContext`:**

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Properties<DateTimeOffset>()
        .HaveConversion<DateTimeOffsetToUnixMillisecondsConverter>();
}
```

### WARNING-01 — `Account.Id` primary key has a public setter; spec says immutable

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Features/Accounts/Account.cs:14`
**Severity:** warning

**Issue:** The XML doc comment on `Id` says _"Synthetic primary key — immutable once assigned"_, but the property declaration is `public Guid Id { get; set; }`, allowing mutation after construction. The `init` accessor enforces the immutability contract at compile time.

**Fix:**

```csharp
public Guid Id { get; init; }
```

---

### WARNING-02 — `BackupAsync()` accepts no `CancellationToken`; file I/O cannot be cancelled

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/IDbBackupService.cs:20`,
`apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/DbBackupService.cs:16`
**Severity:** warning

**Issue:** `BackupAsync()` performs potentially large file I/O via `Stream.CopyToAsync` but exposes no `CancellationToken` parameter. If the user shuts down the app or cancels a sync while a backup is in progress the operation cannot be interrupted. This also violates the repo convention of always propagating cancellation through async I/O paths.

**Fix:**

```csharp
// Interface
Task<Result<bool, ErrorResponse>> BackupAsync(CancellationToken cancellationToken = default);

// Implementation
public async Task<Result<bool, ErrorResponse>> BackupAsync(CancellationToken cancellationToken = default)
{
    ...
    await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    ...
}
```

---

### WARNING-03 — `LinuxAppDataPathProvider` class name is misleading

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/LinuxAppDataPathProvider.cs:12`
**Severity:** warning

**Issue:** The class is named `LinuxAppDataPathProvider` but the implementation uses `Environment.SpecialFolder.LocalApplicationData`, which resolves cross-platform (`~/.local/share/…` on Linux, `%LOCALAPPDATA%\…` on Windows, `~/Library/Application Support/…` on macOS). The class comment itself acknowledges this. A misleading name leads to incorrect DI registrations on non-Linux platforms in future and obscures intent. Rename to `LocalAppDataPathProvider`.

---

### WARNING-04 — Formatting artefact: excessive whitespace in `LinuxAppDataPathProvider`

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/LinuxAppDataPathProvider.cs:15`
**Severity:** warning

**Issue:** Line 15 contains a multi-space gap before the `"AStar.Dev.OneDriveSync"` argument inside the `CombinePath` call, suggesting an edit artefact from a find-replace or reformat operation. With `TreatWarningsAsErrors=true` this won't fail the build, but it will fail a future formatter / style-cop step.

```csharp
// Current (with spurious spaces)
private static readonly string _resolvedPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombinePath(       "AStar.Dev.OneDriveSync");

// Expected
private static readonly string _resolvedPath = Environment
    .GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
    .CombinePath("AStar.Dev.OneDriveSync");
```

---

### WARNING-05 — `AccountConfiguration` does not declare `ValueGeneratedNever()` for the synthetic key

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/Configurations/AccountConfiguration.cs`
**Severity:** warning

**Issue:** EF Core's conventions mark any `Guid` primary key as `ValueGeneratedOnAdd()` (evidenced in the snapshot at line 23). The design intent is a client-generated synthetic GUID. Without an explicit `ValueGeneratedNever()` declaration in the configuration, EF Core may silently overwrite a caller-assigned `Id` with a new GUID in certain provider configurations. The configuration should be explicit.

**Fix:**

```csharp
builder.Property(a => a.Id)
    .ValueGeneratedNever();
```

---

### WARNING-06 — SQL injection risk in `SqliteAssert`

**File:** `apps/desktop/AStar.Dev.OneDriveSync.Tests.Integration/Helpers/SqliteAssert.cs:10`
**Severity:** warning

**Issue:** The `accountId` value is embedded directly into the SQL string via `ToString` interpolation rather than a `SqliteParameter`. Although this is test-only code and the GUID is internally generated, embedding values as literals establishes a bad pattern that — if copied into production code — becomes a security defect. Use parameterized queries consistently.

**Fix:**

```csharp
public static void ChildRowCount(SqliteConnection connection, string tableName, Guid accountId, long expected)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE account_id = $accountId";
    cmd.Parameters.AddWithValue("$accountId", accountId.ToString("D").ToUpperInvariant());
    var actual = (long?)cmd.ExecuteScalar();

    actual.ShouldBe(expected);
}
```

---

### WARNING-07 — `AccountBuilder` fluent methods violate "blank line before `return`" rule

**File:** `apps/desktop/AStar.Dev.OneDriveSync.Tests.Integration/Helpers/AccountBuilder.cs:12-15`
**Severity:** warning

**Issue:** Repo formatting rules require every `return` statement to be preceded by a blank line. Lines 12–15 place the assignment and `return this;` on the same line with no blank line between them. The rule applies in tests and test helpers without exception.

**Fix — expand the single-line bodies:**

```csharp
public AccountBuilder WithId(Guid id)
{
    _id = id;

    return this;
}

public AccountBuilder WithDisplayName(string displayName)
{
    _displayName = displayName;

    return this;
}

public AccountBuilder WithEmail(string email)
{
    _email = email;

    return this;
}

public AccountBuilder WithMicrosoftAccountId(string msId)
{
    _microsoftAccountId = msId;

    return this;
}
```

---

### SUGGESTION-01 — No unique index on `Email` or `MicrosoftAccountId`

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/Configurations/AccountConfiguration.cs`
**Severity:** suggestion

**Issue:** Nothing at the schema level prevents two `Account` rows from being inserted with the same `Email` or the same `MicrosoftAccountId`. The spec test `when_two_accounts_share_the_same_microsoft_account_id_then_each_has_a_distinct_guid` intentionally validates that the synthetic GUID is independent of the Microsoft identity — but this does not mean duplicate `MicrosoftAccountId` values should be allowed. A unique index on `MicrosoftAccountId` would prevent silent double-registration of the same Microsoft account.

**Fix:**

```csharp
builder.HasIndex(a => a.MicrosoftAccountId)
    .IsUnique();
```

---

### SUGGESTION-02 — Raw SQL INSERTs in test bodies embed values via string interpolation

**Files:** `tests/Integration/Persistence/GivenAnAccountWithLinkedRows.cs:19,43-44`,
`tests/Integration/Persistence/GivenAnAccountWithAMicrosoftIdentity.cs:40`,
`tests/Integration/Persistence/GivenTheDatabaseSchema.cs:36-38`
**Severity:** suggestion

**Issue:** Multiple test bodies construct raw `INSERT` SQL by interpolating `Guid.NewGuid()` and `account.Id` as string literals. While the values are not attacker-controlled, the pattern is inconsistent with `SqliteAssert`'s intended direction and establishes a template that will cause problems if the pattern migrates to production helpers. Prefer a small shared helper or parameterized `ExecuteSqlRawAsync` with `SqliteParameter`.

---

### SUGGESTION-03 — Migration file retains `#nullable disable`

**File:** `apps/desktop/AStar.Dev.OneDriveSync/Infrastructure/Persistence/Migrations/20260327203545_InitialCreate.cs:4`
**Severity:** suggestion

**Issue:** The generated migration file has `#nullable disable` at the top. EF Core tooling generates this by default, but because the repo has `<Nullable>enable</Nullable>` globally and `TreatWarningsAsErrors=true`, any future hand-edit to the migration that introduces a nullable warning will be silently suppressed rather than caught at build time. Remove the directive and address any resulting warnings.

---

## Summary

| Severity   | Count  |
| ---------- | ------ |
| error      | 5      |
| warning    | 7      |
| suggestion | 3      |
| **Total**  | **15** |

## Verdict — ❌ Request Changes

Five errors block acceptance:

1. **Stale model snapshot** (ERROR-01) — the moved `Account` entity namespace will cause a runtime EF Core failure.
2. **No startup migration** (ERROR-02) — the core S002 acceptance criterion (`MigrateAsync` at startup) is entirely unimplemented.
3. **No logging anywhere** (ERROR-03) — NF-00 is an unconditional requirement; zero logging infrastructure exists in the production code.
4. **Dead value converter** (ERROR-04) — the `DateTimeOffsetToUnixMillisecondsConverter` is never registered; DB-01 is not satisfied.
5. **`EnsureCreatedAsync` in tests** (ERROR-05) — violates an explicit S002 "never used" rule and gives false test confidence over the migration path.

All five errors must be resolved before this story can be marked complete. The warnings, particularly WARNING-07 (formatting rule) and WARNING-03 (misleading class name), should be addressed in the same pass.
