# S002 — Database Foundation (SQLite / EF Core / Migrations)

**Phase:** MVP
**Area:** Foundation
**Spec refs:** Section 8 (Data Architecture), DB-01, DB-02, DB-03, DB-04, DB-05, DB-07, EH-07, EH-08, NF-17

---

## User Story

As a developer,
I want a single SQLite database with EF Core migrations, correctly placed and initialised at app startup,
So that all features have a reliable, schema-versioned persistence layer from the first story that needs it.

---

## Acceptance Criteria

### Schema & Migrations

- [ ] Single `AppDbContext` in `Infrastructure/Persistence/AppDbContext.cs`
- [ ] Database file stored at shared AStar.Dev path: `~/.local/share/astar-dev/file-data.db` (Linux); abstracted via `IAppDataPathProvider` for cross-platform (DB-04)
- [ ] `astar-dev/` directory created by the app at startup if it does not exist (DB-04)
- [ ] WAL mode (`PRAGMA journal_mode=WAL`) enabled at startup before any reads or writes (DB-05, NF-17)
- [ ] On first launch after upgrade, if `~/.local/share/AStar.Dev.OneDriveSync/file-data.db` exists, copy it to the new shared path before running migrations; leave the original file in place (DB-07)
- [ ] `Database.MigrateAsync()` called at startup — `EnsureCreatedAsync` is **never used**
- [ ] `IDesignTimeDbContextFactory<AppDbContext>` implemented and verified with `dotnet ef migrations add` (DB-03)
- [ ] Initial migration created and committed

### Entity Configuration

- [ ] All entity configurations implemented as `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`
- [ ] `OnModelCreating` uses `ApplyConfigurationsFromAssembly` — no inline `modelBuilder.Entity<T>()` calls (DB-02)

### Accounts Table (PII Isolation — Section 8)

- [ ] `Account` entity: synthetic `Guid` primary key; `DisplayName`, `Email`, `MicrosoftAccountId` columns present **only** in this table
- [ ] All other entities reference `Account` via the synthetic `Guid` FK — no email/name columns elsewhere
- [ ] FK cascade delete configured: deleting an `Account` removes all referencing rows across every table

### DateTimeOffset Storage (DB-01)

- [ ] All `DateTimeOffset` properties stored as Unix milliseconds (`long`) via EF Core value converters
- [ ] Value converter implemented once and reused — not duplicated per entity

### Pre-Sync Backup (EH-07)

- [ ] `IDbBackupService` interface defined; implementation copies `file-data.db` to `file-data.db.bak` before any sync mutation begins
- [ ] Backup is **not** periodic — only triggered on sync start

### Corrupt DB Recovery (EH-08)

- [ ] On startup, if `MigrateAsync` throws, the app catches the exception and routes to a "Database corrupt" recovery flow (UI story S003 wires this up)
- [ ] Recovery option: user can choose "Start Fresh" — deletes `file-data.db` and restarts migration

### Data Integrity Tests

- [ ] **Integration test**: account deletion removes all rows referencing that account across every table; no orphaned rows remain
- [ ] **Integration test**: inserting a row into any non-Account table with a real email/name instead of a synthetic `Guid` FK fails at the schema level
- [ ] **Integration test**: the synthetic account `Guid` FK is independent of any Microsoft account detail (changing `MicrosoftAccountId` does not affect FK relationships)
- [ ] **Unit test**: `DateTimeOffset` round-trip via value converter preserves UTC offset
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `IAppDataPathProvider` returns `~/.local/share/AStar.Dev.OneDriveSync/` on Linux; Windows/macOS paths handled for future-proofing but not tested in MVP
- EF Core `UseSnakeCaseNamingConvention()` is optional but must be consistent — choose and document in code
- Logging: DB migration success/failure logged at `Information`/`Error` respectively (NF-00)
- `Result<T>` from `AStar.Dev.Functional.Extensions` used in `IDbBackupService` return type (NF-16)

---

## Dependencies

- S001 (project scaffolding)
