## Context

`FileClassificationRule` records exist in the database and drive how synced files are tagged. `IFileClassificationRuleRepository` exposes only `GetAllAsync`; there is no mutation surface. The desktop app uses CommunityToolkit.Mvvm (`ObservableObject`, `[RelayCommand]`) and Avalonia AXAML views. `SettingsView` is a `ScrollViewer`-hosted `StackPanel` of card sections — the established pattern for app-level configuration.

## Goals / Non-Goals

**Goals:**
- Extend the repository with `AddAsync` and `DeleteAsync`.
- Add a dedicated `FileClassificationRulesViewModel` with an observable rule list, an add command, and a per-row delete command.
- Add a `FileClassificationRuleRowViewModel` to carry the entity `Id` needed for deletion alongside display properties.
- Add a `FileClassificationsView` UserControl that renders the list and an inline add form.
- Embed the new view in `SettingsView` as a new card section; every mutation persists immediately with no separate Save button.

**Non-Goals:**
- Inline editing of existing rules (add + delete covers all mutations needed).
- Bulk import / export.
- Validation beyond "Keywords and Level1 are non-empty before enabling Add".

## Decisions

### 1 — Separate ViewModel, not extending SettingsViewModel
`SettingsViewModel` owns appearance, sync policy, and account paths. Adding classification rule state to it would violate SRP and push it over the 300-line guideline. `FileClassificationRulesViewModel` is injected into `SettingsViewModel` as a constructor-injected dependency and exposed as a public property for binding.

**Alternative considered**: Embed state directly in `SettingsViewModel`. Rejected — couples unrelated concerns and bloats an already-busy class.

### 2 — ObservableCollection of row ViewModels
The list renders via `ItemsControl` binding. Each row needs both display data and a `DeleteCommand` scoped to that row's database `Id`. A `FileClassificationRuleRowViewModel(int id, FileClassificationRule rule)` carries both, keeping the delete command close to the data it acts on.

**Alternative considered**: Pass id as `CommandParameter` from AXAML. Rejected — leaks persistence concerns into the view and makes the ViewModel harder to test.

### 3 — Repository mutation surface: entity Id for delete
`FileClassificationRule` (the domain record) carries no `Id`; identity lives on `FileClassificationRuleEntity`. `DeleteAsync(int id)` takes the raw EF `Id`. `AddAsync` receives a `FileClassificationRule` domain record and returns the assigned `int Id`, allowing the caller to construct the row ViewModel immediately without a reload.

### 4 — Immediate persistence via fire-and-await in commands
Each `[RelayCommand]` awaits the repository call before updating the observable collection, keeping UI state consistent with DB state. No optimistic updates, no deferred save — keeps the implementation simple and matches the "persist immediately" requirement.

### 5 — Keywords entry as comma-separated string
The entity stores keywords pipe-delimited (`|`). The UI accepts a freeform comma-separated string; the ViewModel splits, trims, and deduplicates before passing `IReadOnlyList<string>` to the factory. This is the simplest input affordance that avoids a multi-tag chip control.

## Risks / Trade-offs

- **Long rule list performance** → `ItemsControl` without virtualisation may scroll slowly at scale. Acceptable now; a `ListBox` with virtualisation is a drop-in replacement if needed.
- **No undo for delete** → rule is immediately removed from DB. Low risk given the classification rules are small in number and easily re-added.
- **AddAsync returns Id** → requires a migration of the repository interface; callers only use `GetAllAsync` today so no other consumers are affected.

## Migration Plan

1. Extend `IFileClassificationRuleRepository` and `FileClassificationRuleRepository` with `AddAsync` / `DeleteAsync`.
2. Update DI registration if the interface is registered by type (likely `AddScoped` — no change needed, same concrete class).
3. Add `FileClassificationRuleRowViewModel`, `FileClassificationRulesViewModel`.
4. Add `FileClassificationsView.axaml` + code-behind.
5. Inject `FileClassificationRulesViewModel` into `SettingsViewModel`; expose as property; embed view in `SettingsView`.
6. Call `LoadAsync()` wherever `SettingsViewModel` is initialised (same site as `LoadAccounts`).

No database schema changes. No migration script required.
