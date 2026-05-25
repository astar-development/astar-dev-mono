## 1. Repository — tests first (TDD red)

- [x] 1.1 Write failing test: `GivenAFileClassificationRuleRepository.when_add_async_called_then_rule_is_persisted_and_id_returned`
- [x] 1.2 Write failing test: `GivenAFileClassificationRuleRepository.when_delete_async_called_with_existing_id_then_rule_is_removed`
- [x] 1.3 Write failing test: `GivenAFileClassificationRuleRepository.when_delete_async_called_with_nonexistent_id_then_no_exception`
- [x] 1.4 Commit red tests

## 2. Repository — production code

- [x] 2.1 Add `Task<int> AddAsync(FileClassificationRule rule, CancellationToken cancellationToken = default)` to `IFileClassificationRuleRepository`
- [x] 2.2 Add `Task DeleteAsync(int id, CancellationToken cancellationToken = default)` to `IFileClassificationRuleRepository`
- [x] 2.3 Implement `AddAsync` in `FileClassificationRuleRepository` — map domain record → entity, add, save, return `entity.Id`
- [x] 2.4 Implement `DeleteAsync` in `FileClassificationRuleRepository` — find by Id, delete if found, save; no-op if not found
- [x] 2.5 Run tests — all three new tests green; existing `GetAllAsync` test still passes

## 3. ViewModel — tests first (TDD red)

- [x] 3.1 Write failing test: `GivenAFileClassificationRulesViewModel.when_load_async_called_then_rules_collection_populated`
- [x] 3.2 Write failing test: `GivenAFileClassificationRulesViewModel.when_add_command_executed_then_rule_persisted_and_added_to_collection`
- [x] 3.3 Write failing test: `GivenAFileClassificationRulesViewModel.when_keywords_empty_then_add_command_disabled`
- [x] 3.4 Write failing test: `GivenAFileClassificationRulesViewModel.when_level1_empty_then_add_command_disabled`
- [x] 3.5 Write failing test: `GivenAFileClassificationRulesViewModel.when_add_succeeds_then_form_inputs_cleared`
- [x] 3.6 Write failing test: `GivenAFileClassificationRulesViewModel.when_delete_command_executed_then_rule_deleted_and_removed_from_collection`
- [x] 3.7 Commit red tests

## 4. ViewModel — production code

- [x] 4.1 Create `FileClassificationRuleRowViewModel` — properties: `Id` (int), `Keywords` (display string), `Level1`, `Level2`, `Level3`, `IsSpecial`; `DeleteCommand` (`[RelayCommand]` calling parent's delete method)
- [x] 4.2 Create `FileClassificationRulesViewModel(IFileClassificationRuleRepository repository)` inheriting `ObservableObject`
- [x] 4.3 Add `ObservableCollection<FileClassificationRuleRowViewModel> Rules`
- [x] 4.4 Add `[ObservableProperty]` for `NewKeywords`, `NewLevel1`, `NewLevel2`, `NewLevel3`, `NewIsSpecial`
- [x] 4.5 Add `LoadAsync(CancellationToken ct)` — loads `GetAllAsync`, clears and repopulates `Rules`
- [x] 4.6 Add `[RelayCommand(CanExecute = nameof(CanAdd))]` `AddAsync` — split/trim Keywords, call `AddAsync`, append row, clear form
- [x] 4.7 Implement `CanAdd` — returns `true` when `NewKeywords` and `NewLevel1` are non-empty/non-whitespace
- [x] 4.8 Wire `NotifyCanExecuteChangedFor` on `NewKeywords` and `NewLevel1` properties
- [x] 4.9 Add `DeleteRuleAsync(int id)` — calls `repository.DeleteAsync`, removes matching row from `Rules`
- [x] 4.10 Run tests — all ViewModel tests green
- [x] 4.11 Inject `FileClassificationRulesViewModel` into `SettingsViewModel`; expose as public property `ClassificationRules`
- [x] 4.12 Call `await ClassificationRules.LoadAsync(ct)` at the same site `SettingsViewModel.LoadAccounts` is called

## 5. View (AXAML)

- [x] 5.1 Create `FileClassificationsView.axaml` + code-behind in `Settings/` (or new `Classifications/` folder)
- [x] 5.2 Render `Rules` as `ItemsControl` — each row shows Keywords, Level1, Level2, Level3, IsSpecial chip, and a Delete button bound to `DeleteCommand`
- [x] 5.3 Add empty-state `TextBlock` visible when `Rules` is empty
- [x] 5.4 Add add-form inputs: Keywords `TextBox`, Level1 `TextBox`, Level2 `TextBox`, Level3 `TextBox`, IsSpecial `CheckBox`, Add `Button` bound to `AddCommand`
- [x] 5.5 Embed `FileClassificationsView` in `SettingsView.axaml` as a new "File classifications" card section, following the existing `Border + StackPanel` card pattern

## 6. Build and verify

- [x] 6.1 `dotnet build` — zero errors, zero warnings
- [x] 6.2 `dotnet test` — all tests pass (excluding committed red tests that are now green)
- [ ] 6.3 Run the desktop app, open Settings, confirm rules list loads, add a rule, verify it persists on restart, delete a rule, verify it is gone
