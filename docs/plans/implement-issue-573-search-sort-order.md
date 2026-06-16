# Plan: Apply SearchSortOrder in SyncedItemRepository.SearchAsync (Issue #573)

## Context

Issue #572 added `SearchSortOrder` enum and `SortOrder` property to `SyncedItemSearchCriteria`. This plan (phase 2 of 4) wires that enum into the EF Core query in `SearchAsync` so ordering happens at DB level — not in memory — for efficiency on large result sets.

## Changes

### 1. `Data/Repositories/SyncedItemRepository.cs`

After all `.Where(...)` clauses (including the `DuplicatesOnly` block), before the `.Select(...)` projection, add a switch expression:

```csharp
query = criteria.SortOrder switch
{
    SearchSortOrder.NameDescending => query.OrderByDescending(i => i.RemotePath),
    SearchSortOrder.SizeAscending  => query.OrderBy(i => i.SizeInBytes),
    SearchSortOrder.SizeDescending => query.OrderByDescending(i => i.SizeInBytes),
    _                              => query.OrderBy(i => i.RemotePath)
};
```

Default arm `_` covers `NameAscending` and any future values.

### 2. `appsettings.json` — version bump (feature rule from app CLAUDE.md)

```json
"ApplicationVersion": "0.12.0"
```

(0.11.2 → 0.12.0: feature = minor bump)

### 3. 4 new unit tests in existing class

**File:** `apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Data/Repositories/GivenASyncedItemRepository.cs`

Add 4 tests using the existing `CreateInMemoryFactory()` / `FileItem()` helpers:

Seed: 3 items, same `AccountId("user-1")`, all files (not folders):
- `/files/bravo.txt`, 2000 bytes
- `/files/alpha.txt`, 3000 bytes  
- `/files/charlie.txt`, 1000 bytes

| Test method | SortOrder | Expected path order |
|-------------|-----------|---------------------|
| `when_search_sort_order_is_name_ascending_then_results_are_ordered_a_to_z` | `NameAscending` | alpha, bravo, charlie |
| `when_search_sort_order_is_name_descending_then_results_are_ordered_z_to_a` | `NameDescending` | charlie, bravo, alpha |
| `when_search_sort_order_is_size_ascending_then_results_are_ordered_smallest_first` | `SizeAscending` | charlie (1000), bravo (2000), alpha (3000) |
| `when_search_sort_order_is_size_descending_then_results_are_ordered_largest_first` | `SizeDescending` | alpha (3000), bravo (2000), charlie (1000) |

Use `SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), sortOrder: <value>)`.

Assert with `results[0].RemotePath.ShouldBe(...)` etc.

## Files to modify

| File | Change |
|------|--------|
| `apps/desktop/AStar.Dev.OneDrive.Sync.Client/Data/Repositories/SyncedItemRepository.cs` | Add sort switch before `.Select(...)` |
| `apps/desktop/AStar.Dev.OneDrive.Sync.Client/appsettings.json` | Version 0.11.2 → 0.12.0 |
| `apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Data/Repositories/GivenASyncedItemRepository.cs` | 4 new sort-order test methods |

## Reuse

- `CreateInMemoryFactory()` — existing helper, no changes
- `FileItem(...)` — existing helper, no changes
- `SyncedItemSearchCriteriaFactory.Create(...)` — pass `sortOrder:` named arg

## Verification

1. `dotnet build` — zero warnings/errors
2. `dotnet test --filter "when_search_sort_order"` — all 4 pass
3. `dotnet test` — no regressions
