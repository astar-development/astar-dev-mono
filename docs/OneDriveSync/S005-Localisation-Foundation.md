# S005 — Localisation Foundation

**Phase:** MVP  
**Area:** Foundation  
**Spec refs:** LO-01 to LO-07

---

## User Story

As a developer and as a user,  
I want all user-facing strings externalised into `.resx` files with `en-GB` as the MVP locale and locale selection in Settings,  
So that the app is internationalisation-ready from day one and users get locale-appropriate date/time formatting.

---

## Acceptance Criteria

### Infrastructure (LO-01, LO-02)
- [ ] All user-facing strings (labels, tooltips, error messages, notification text, log messages where practicable) stored in `.resx` resource files — **no hardcoded strings in AXAML or C# UI code**
- [ ] MVP locale: `en-GB` — this is the fallback and only fully translated locale
- [ ] `ILocalisationService` interface in `Infrastructure/Localisation/`; `LocalisationService` is the implementation

### Locale Selection (LO-03, LO-04)
- [ ] Settings view exposes a locale selector (dropdown); selection applies globally and immediately
- [ ] On first launch, OS locale is detected; if not `en-GB` (the only supported locale in MVP), `en-GB` is applied silently
- [ ] Selected locale persisted in the SQLite `AppSettings` table

### Date/Time Formatting (LO-07)
- [ ] "Last synced" display: relative format for < 1 hour ("5 minutes ago"); absolute format for ≥ 1 hour ("Today at 14:32" / "25 Mar at 09:15")
- [ ] All date/time formatting respects the active locale setting
- [ ] `IRelativeTimeFormatter` interface + `RelativeTimeFormatter` implementation — testable independently

### Conflict Rename Format (LO-06)
- [ ] "Keep Both" conflict rename suffix uses UTC-formatted string: `yyyy-MM-ddTHHmmssZ` — this format is locale-invariant (see CR-04)

### Tests
- [ ] **Unit test**: `RelativeTimeFormatter` — values < 1 hour return relative strings; ≥ 1 hour return absolute strings; edge cases at exactly 1 hour
- [ ] **Unit test**: conflict rename suffix is locale-invariant (same output regardless of active locale)
- [ ] **Integration test**: locale change persists and is restored correctly
- [ ] Static analysis / test: no hardcoded UI strings in `*.axaml` or `*ViewModel.cs` files (string literals in UI code fail the test)
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `ILocalisationService` registered as **singleton** — locale is app-wide
- Additional locales (LO-08) are Post-MVP; the `LocalisationService` must support adding them without structural change
- Logging strings: "where practicable" — at minimum, all log messages visible to Casual users must be in `.resx`; Power User verbose logs may use inline strings (document this decision)
- NF-16: `Result<T>` on locale persistence operations

---

## Dependencies

- S001 (project scaffolding)
- S002 (database — locale stored in SQLite)
- S003 (navigation shell — locale applied before window shown)
