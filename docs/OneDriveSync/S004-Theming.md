# S004 — Theming (Light / Dark / Auto)

**Phase:** MVP  
**Area:** Foundation  
**Spec refs:** TH-01 to TH-06, Section 8 (theme stored in SQLite)

---

## User Story

As a user,  
I want to choose Light, Dark, or Auto (OS-following) theme and have it apply instantly everywhere without restarting,  
So that the app looks comfortable in my environment and respects my OS preference automatically.

---

## Acceptance Criteria

### Theme Modes (TH-01)
- [ ] Three modes supported: **Light**, **Dark**, **Auto**
- [ ] "Auto" observes OS theme via Avalonia's `PlatformThemeVariant` or equivalent; responds to OS theme changes in real-time without restart (TH-05)

### Runtime Switching (TH-03)
- [ ] Theme change applies **immediately** — resource dictionaries are swapped dynamically; no restart required
- [ ] All open views reflect the new theme without navigation

### Hardcoded Colours Forbidden (TH-04)
- [ ] Zero hardcoded colour values (`#RRGGBB`, named colours) in any `.axaml` file — all colours reference resource keys
- [ ] CI build (or a unit test) statically asserts no hardcoded colours exist in AXAML files

### Persistence (TH-02)
- [ ] Theme selection stored in the SQLite `AppSettings` table (single-row settings record)
- [ ] Theme loaded and applied before the main window is shown (no flash of wrong theme)

### Extensibility (TH-06)
- [ ] Adding a new theme requires only: a new `ResourceDictionary` file + registration in `ThemeService` — no other code changes
- [ ] `IThemeService` interface in `Infrastructure/Theming/`; `ThemeService` is the implementation

### Tests
- [ ] **Unit test**: `ThemeService.SetTheme(ThemeMode.Dark)` raises a `ThemeChanged` observable/event
- [ ] **Unit test**: `ThemeService.SetTheme(ThemeMode.Auto)` subscribes to OS changes (use mock platform provider)
- [ ] **Integration test**: theme persists across a simulated restart (write to DB; read back and assert match)
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `IThemeService` registered as a **singleton** — theme is app-wide state
- Use `ReactiveUI` `WhenAnyValue` to propagate theme changes to `MainWindowViewModel`
- Post-MVP items (TH-07, TH-08, TH-09) are explicitly **out of scope** for this story
- Logging: theme changes logged at `Debug` level (NF-00)
- `Result<T>` used for DB read/write operations in `ThemeService` (NF-16)

---

## Dependencies

- S001 (project scaffolding)
- S002 (database — theme stored in SQLite)
- S003 (navigation shell — must apply theme before window shown)
