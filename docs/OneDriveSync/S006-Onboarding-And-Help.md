# S006 — Onboarding & Help

**Phase:** MVP  
**Area:** Features/Onboarding, Features/Help  
**Spec refs:** OH-01 to OH-06, Section 5 (User Type), Section 7 (Help nav item)

---

## User Story

As a first-time user,  
I want a welcome screen that explains the app, lets me choose my user type, and guides me to add my first account,  
So that I can get started confidently without reading external documentation.

---

## Acceptance Criteria

### Welcome Screen (OH-01, OH-03)
- [ ] First launch (no accounts in DB) routes directly to the onboarding view before the main nav rail is interactive
- [ ] Welcome screen explains what the app does in plain language
- [ ] Prominent "Add your first account" CTA button visible
- [ ] "Skip" option present — routes directly to the Accounts view without completing onboarding (OH-03)

### External Help Link (OH-02)
- [ ] Onboarding includes a link that opens an external markdown help file in the system browser
- [ ] URL is configurable (not hardcoded) — stored in app config or resource file

### User Type Selection (OH-04, Section 5)
- [ ] User type selection (Casual / Power User) presented **before** the add-account wizard CTA during onboarding
- [ ] Casual is the default selection
- [ ] Switching to Power User during onboarding shows confirmation: "This unlocks advanced settings that can affect sync performance if misconfigured" — user must confirm
- [ ] Switching to Casual requires no confirmation
- [ ] Selection persisted in SQLite `AppSettings` table
- [ ] User type selection also accessible any time in Settings (wired up in S015)

### Help Nav Item (OH-05, OH-06)
- [ ] Help icon in nav rail (item 7) opens a view that replays the onboarding content (identical content in MVP)
- [ ] Help view accessible at all times from the nav rail — even after onboarding is complete

### Subsequent Launches
- [ ] If ≥ 1 account exists in DB, onboarding is **not** shown on launch — app goes directly to Dashboard
- [ ] Onboarding can be replayed via the Help nav item at any time

### Tests
- [ ] **Unit test**: `OnboardingViewModel` — if no accounts, `ShouldShowOnboarding` is `true`; if accounts exist, `false`
- [ ] **Unit test**: User type toggle — Casual → Power User emits confirmation request; Power User → Casual does not
- [ ] **Unit test**: Skipping onboarding marks it as complete and routes to Accounts
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `OnboardingViewModel` is scoped (one instance per nav activation) — not singleton
- User type is app-level singleton state (`IUserTypeService`) referenced by all features that show/hide advanced controls
- Logging: onboarding completion and user type selection logged at `Information` (NF-00)
- NF-15: "Add your first account" CTA may be disabled if authentication is unavailable (future-proof hook)

---

## Implementation Constraints

- **`x:DataType` required on all DataTemplates** — `AvaloniaUseCompiledBindingsByDefault=true` is global; every `DataTemplate` in onboarding views must carry `x:DataType` or the build will produce compiled-binding errors.
- **Wizard VM is transient, resolved via factory** — `OnboardingViewModel` is scoped (one per activation); it must be resolved via a `Func<OnboardingViewModel>` factory registered in DI, not via `IServiceProvider.GetRequiredService<>()` directly in code-behind. Direct `GetRequiredService<>()` on a transient returns a new instance but bypasses lifetime validation.
- **Dialogs via Avalonia dialog API** — the Power User confirmation dialog must use Avalonia's dialog infrastructure (e.g. a `UserControl`-based dialog or the `MessageBoxManager` from the Avalonia community toolkit). `System.Windows.MessageBox` and `WinRT.MessageDialog` are platform-only and will not compile cross-platform.
- **`UserControl.Styles` scope** — any new custom `UserControl` controls in this story that need to style their own root element's state (hover, disabled, focus) must handle it via `AddClassHandler` in code-behind, not via `Style Selector="UserControl:disabled"` inside `UserControl.Styles`. See S003 retrospective.
- **Register Help nav item** — `ShellServiceExtensions.RegisterAvailableFeatures()` must call `service.Register(NavSection.Help)` when this story ships.
---

## Dependencies

- S001 (project scaffolding)
- S002 (database — user type persisted)
- S003 (navigation shell — onboarding intercepts first launch)
- S005 (localisation — all onboarding strings in `.resx`)
