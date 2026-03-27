# S002 — Database Foundation (SQLite / EF Core / Migrations)

**Phase:** MVP  
**Area:** Foundation  
**Spec refs:** Section 8 (Data Architecture), DB-01, DB-02, DB-03, EH-07, EH-08

---

## User Story

As a developer,  
I want a single SQLite database with EF Core migrations, correctly placed and initialised at app startup,  
So that all features have a reliable, schema-versioned persistence layer from the first story that needs it.

---

## Acceptance Criteria

### Schema & Migrations
- [ ] Single `AppDbContext` in `Infrastructure/Persistence/AppDbContext.cs`
- [ ] Database file stored at platform-appropriate path: `~/.local/share/AStar.Dev.OneDriveSync/data.db` (Linux); abstracted via `IAppDataPathProvider` for cross-platform
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
- [ ] `IDbBackupService` interface defined; implementation copies `data.db` to `data.db.bak` before any sync mutation begins
- [ ] Backup is **not** periodic — only triggered on sync start

### Corrupt DB Recovery (EH-08)
- [ ] On startup, if `MigrateAsync` throws, the app catches the exception and routes to a "Database corrupt" recovery flow (UI story S003 wires this up)
- [ ] Recovery option: user can choose "Start Fresh" — deletes `data.db` and restarts migration

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
