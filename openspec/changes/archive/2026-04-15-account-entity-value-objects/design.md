## Context

`AStar.Dev.OneDrive.Sync.Client` stores OneDrive account data in SQLite via EF Core. All domain identifiers (`AccountId`, `OneDriveFolderId`, `OneDriveItemId`) and the local sync path are currently raw `string` properties on EF entities and domain models. This allows callers to pass a folder ID where an account ID is expected without any compile-time error.

The repo already provides `AStar.Dev.Source.Generators` + `AStar.Dev.Source.Generators.Attributes` which generate strongly-typed `record struct` ID wrappers via `[StrongId]`. The app project currently has no reference to these packages.

## Goals / Non-Goals

**Goals:**
- Introduce compile-time type safety for `AccountId`, `OneDriveFolderId`, `OneDriveItemId`
- Introduce `LocalSyncPath` as a validated value object
- Keep the SQLite schema unchanged (EF value converters map to the same underlying column type)
- No breaking change to the SQLite database file — no destructive migration

**Non-Goals:**
- Refactoring unrelated entities or models outside the OneDrive sync domain
- Changing authentication, Graph API, or sync logic
- Introducing a new database schema version (column types stay as `TEXT`)

## Decisions

### D1 — Use `[StrongId(typeof(string))]` for all three identifier types

`AccountId`, `OneDriveFolderId`, and `OneDriveItemId` are Microsoft Graph object IDs: stable opaque strings. `[StrongId(typeof(string))]` generates a `partial record struct` with implicit conversions to/from `string` disabled by default, ensuring callers are explicit. Source-generator approach keeps the code DRY.

**Alternative considered:** Hand-written `readonly record struct` — rejected; more boilerplate for zero additional benefit given the generator already exists in the repo.

### D2 — `LocalSyncPath` as a manual immutable record, not `[StrongId]`

A sync path has domain rules (must not be null/empty; normalised directory separator) that belong in a factory method. `[StrongId]` generates plain wrapping with no validation. A `record` with a private constructor and a static `Create` factory returning `Result<LocalSyncPath>` encodes the invariant.

**Alternative considered:** Keep as `string` with validation at service layer — rejected; validation belongs at the type boundary, not scattered across callers.

### D3 — EF Core value converters, not shadow properties or owned entities

EF 10 value converters are the correct mechanism for mapping a value object to a scalar column with no schema change. Each converter maps `AccountId ↔ string`, etc. Converters are registered in the existing `IEntityTypeConfiguration<T>` classes.

**Alternative considered:** Store as BLOB or int — rejected; Graph IDs are strings; changing column type would require a destructive migration.

### D4 — Unwrap to primitive only at persistence and display boundaries

ViewModels receive typed values from repositories. They unwrap to `string` only when binding to text properties in AXAML or constructing Graph API calls. Unwrapping inside business logic is a code-smell and will be flagged in review.

### D5 — New EF migration for entity changes

Even though column types do not change, the EF model snapshot must reflect the new CLR types. A migration is required to update the snapshot; the generated SQL will be a no-op (no `ALTER TABLE`).

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Large call-site blast radius — many ViewModels reference `.Id` / `.AccountId` as strings | Compiler errors guide the fix; all callers in one app project, so no cross-repo breakage |
| EF migration required even for no-op schema change | Migration is generated + reviewed before merge; SQLite does not support `ALTER COLUMN` anyway so risk is low |
| Source generator version drift | Project reference (not package reference) used locally; CI resolves same source |

## Migration Plan

1. Add project references to source generators in `.csproj`
2. Add new value-object source files (ID structs + `LocalSyncPath` record)
3. Update all entity properties to use new types
4. Update EF configurations to register value converters
5. Update `OneDriveAccount` domain model
6. Update repository interfaces and implementations
7. Update all ViewModels at call sites (compiler errors as guide)
8. `dotnet ef migrations add StronglyTypedIds` — verify generated SQL is no-op
9. `dotnet build` zero errors/warnings; `dotnet test` all green

## Open Questions

- None — all decisions resolved above.
