# S015 — Settings View

**Phase:** MVP  
**Area:** Features/Settings  
**Spec refs:** Section 5 (User Type), Section 7 (Settings nav item), TH-01 (theme selector), LO-03 (locale selector), ST-03 (notification toggle), OH-04 (user type editable in Settings)

---

## User Story

As a user,  
I want a Settings screen where I can change the app theme, language, user type, and notification preferences,  
So that the app behaves and looks the way I prefer.

---

## Acceptance Criteria

### Theme Setting (TH-01)
- [ ] Theme selector: Light / Dark / Auto — wired to `IThemeService` (S004)
- [ ] Change applies immediately — no save button required for theme

### Locale Setting (LO-03)
- [ ] Locale selector: `en-GB` (MVP only); structured to support future locales (LO-08 Post-MVP)
- [ ] Change applies globally and immediately

### User Type Setting (Section 5, OH-04)
- [ ] Toggle: Casual / Power User
- [ ] Switching to **Power User** requires confirmation dialog: "This unlocks advanced settings that can affect sync performance if misconfigured"
- [ ] Switching to **Casual** applies immediately without confirmation
- [ ] Change immediately shows/hides Power User controls across the entire app (Accounts settings, Log Viewer detail, etc.)

### Notifications Toggle (ST-03)
- [ ] Single on/off toggle: "Enable OS notifications"
- [ ] Default: on
- [ ] Setting persisted to SQLite

### Settings Persistence
- [ ] All settings (except theme and locale, which apply immediately) saved on explicit "Save" or on field change — document which approach is used consistently

### Tests
- [ ] **Unit test**: `SettingsViewModel` — user type switch to Power User emits confirmation request; Casual switch does not
- [ ] **Unit test**: `SettingsViewModel` — theme change invokes `IThemeService.SetTheme()`
- [ ] **Integration test**: settings saved and restored after simulated restart
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `SettingsViewModel` is **scoped** — recreated on navigation
- NF-15: locale selector shows `en-GB` only; additional locales greyed out with tooltip "Coming in a future update" (or hidden — document the choice)
- NF-16: persistence operations return `Result<Unit>`
- `IUserTypeService` singleton updated immediately on user type change — all subscribed ViewModels update reactively

---

## Implementation Constraints

- The Power User confirmation dialog must use the Avalonia dialog API (`ShowDialog<T>`) — never `System.Windows.MessageBox` or any platform-native dialog. The parent `Window` reference must be injected or resolved via a `IWindowService` abstraction so `SettingsViewModel` remains testable without a live UI.
- User type change must propagate reactively to all subscribed ViewModels. Every subscriber must call `ObserveOn(RxApp.MainThreadScheduler)` before mutating any UI-bound property; failing to do so causes cross-thread `ObservableCollection` exceptions that are difficult to reproduce.
- `x:DataType` is mandatory on every `DataTemplate` in `SettingsView.axaml`; compiled bindings will silently fail without it.
- Register the Settings nav item in `ShellServiceExtensions` only when this story ships (NF-15); until then the nav item remains disabled.

---

## Dependencies

- S001 (project scaffolding)
- S002 (database — settings stored in SQLite)
- S003 (navigation shell — Settings nav item)
- S004 (theming)
- S005 (localisation)
- S006 (onboarding — user type also set there)
