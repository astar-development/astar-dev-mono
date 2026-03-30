# S002 — Database Foundation (SQLite / EF Core / Migrations)

**Phase:** MVP
**Area:** Foundation
**Spec refs:** Section 8 (Data Architecture), AM-12, AM-13, AM-14, AM-15, DB-01, DB-02, DB-03, DB-04, DB-05, DB-06, DB-07, EH-07, EH-08, NF-17

---

## User Story

As a developer,
I want a single SQLite database with EF Core migrations, correctly placed and initialised at app startup,
So that all features have a reliable, schema-versioned persistence layer from the first story that needs it.

---

## Acceptance Criteria

### Schema & Migrations

- [x] Single `AppDbContext` in `Infrastructure/Persistence/AppDbContext.cs`
- [x] Database file stored at shared AStar.Dev path: `~/.local/share/astar-dev/file-data.db` (Linux); abstracted via `IAppDataPathProvider` for cross-platform (DB-04)
- [x] `astar-dev/` directory created by the app at startup if it does not exist (DB-04)
- [x] WAL mode (`PRAGMA journal_mode=WAL`) enabled at startup before any reads or writes (DB-05, NF-17)
- [x] On first launch after upgrade, if `~/.local/share/AStar.Dev.OneDriveSync/file-data.db` exists, copy it to the new shared path before running migrations; leave the original file in place (DB-07)
- [x] `Database.MigrateAsync()` called at startup — `EnsureCreatedAsync` is **never used**
- [x] `IDesignTimeDbContextFactory<AppDbContext>` implemented and verified with `dotnet ef migrations add` (DB-03)
- [x] Initial migration created and committed

### Entity Configuration

- [x] All entity configurations implemented as `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`
- [x] `OnModelCreating` uses `ApplyConfigurationsFromAssembly` — no inline `modelBuilder.Entity<T>()` calls (DB-02)

### Accounts Table (PII Isolation — Section 8)

- [x] `Account` entity: synthetic `Guid` primary key; `DisplayName`, `Email`, `MicrosoftAccountId` columns present **only** in this table
- [x] All other entities reference `Account` via the synthetic `Guid` FK — no email/name columns elsewhere
- [x] FK cascade delete configured: deleting an `Account` removes all referencing rows across every table

### DateTimeOffset Storage (DB-01)

- [x] All `DateTimeOffset` properties stored as Unix milliseconds (`long`) via EF Core value converters
- [x] Value converter implemented once and reused — not duplicated per entity

### File Metadata Table (AM-12–AM-15, DB-06)

- [x] `SyncedFileMetadata` entity defined with the following columns:
    - `Id` — `long` (auto-increment PK)
    - `AccountId` — `Guid` (FK → `Account.Id`; cascade delete)
    - `RemoteItemId` — `string` (OneDrive item ID)
    - `RelativePath` — `string` (path relative to the account's local sync root)
    - `FileName` — `string`
    - `FileSizeBytes` — `long`
    - `Sha256Checksum` — `string` (hex-encoded)
    - `LastModifiedUtc` — `long` (Unix milliseconds — consistent with DB-01)
    - `CreatedUtc` — `long` (Unix milliseconds)
- [x] `IEntityTypeConfiguration<SyncedFileMetadata>` in `Infrastructure/Persistence/Configurations/`
- [x] FK cascade delete configured: deleting an `Account` removes all `SyncedFileMetadata` rows for that account (DB-06)
- [x] When the per-account AM-12 flag is disabled, existing `SyncedFileMetadata` rows are **retained** — no automatic deletion (DB-06)

### Pre-Sync Backup (EH-07)

- [x] `IDbBackupService` interface defined; implementation copies `file-data.db` to `file-data.db.bak` before any sync mutation begins
- [x] Backup is **not** periodic — only triggered on sync start

### Corrupt DB Recovery (EH-08)

- [x] On startup, if `MigrateAsync` throws, the app catches the exception and routes to a "Database corrupt" recovery flow (UI story S003 wires this up)
- [ ] Recovery option: user can choose "Start Fresh" — deletes `file-data.db` and restarts migration

### Data Integrity Tests

- [x] **Integration test**: account deletion removes all rows referencing that account across every table; no orphaned rows remain
- [x] **Integration test**: account deletion removes all `SyncedFileMetadata` rows for that account (cascade delete)
- [x] **Integration test**: inserting a row into any non-Account table with a real email/name instead of a synthetic `Guid` FK fails at the schema level
- [x] **Integration test**: the synthetic account `Guid` FK is independent of any Microsoft account detail (changing `MicrosoftAccountId` does not affect FK relationships)
- [x] **Unit test**: `DateTimeOffset` round-trip via value converter preserves UTC offset
- [ ] **Unit test**: file metadata is only written to `SyncedFileMetadata` when the per-account AM-12 flag is ON — deferred to AM-12 feature story (requires account settings service)
- [x] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `IAppDataPathProvider` returns `~/.local/share/astar-dev/` on Linux; `%LOCALAPPDATA%\astar-dev\` on Windows; `~/Library/Application Support/astar-dev/` on macOS — the `astar-dev/` directory is shared across all AStar.Dev applications (DB-04)
- EF Core `UseSnakeCaseNamingConvention()` is optional but must be consistent — choose and document in code
- Logging: DB migration success/failure logged at `Information`/`Error` respectively (NF-00)
- `Result<T>` from `AStar.Dev.Functional.Extensions` used in `IDbBackupService` return type (NF-16)

---

## Dependencies

- S001 (project scaffolding)
