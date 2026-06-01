# Plan: Add Hacker Theme to AStar.Dev.OneDrive.Sync.Client

## Context

The app supports Light, Dark, and System themes. The user wants a fixed "Hacker" theme — phosphor green on black — accessible from the Settings UI like the existing three. This also surfaces a pre-existing bug: theme button labels in SettingsView.axaml are hardcoded English strings rather than flowing through the localisation service. That bug is fixed here as part of the same change, wiring all four theme buttons through `ILocalizationService`.

---

## Palette: Hacker (Phosphor Green on Black)

### Surface colours
| Key | Value | Role |
|-----|-------|------|
| `BackgroundPrimary` | `#0A0A0A` | Main window background |
| `BackgroundSecondary` | `#0F140F` | Cards, panels |
| `BackgroundTertiary` | `#141A14` | Slightly raised surfaces |
| `BackgroundRail` | `#080A08` | Icon rail |
| `BackgroundAccountPanel` | `#0D120D` | Account list panel |
| `BackgroundStatusBar` | `#060806` | Status bar |
| `BackgroundHover` | `#1A221A` | Hover state |
| `BackgroundActive` | `#1E281E` | Pressed/active |
| `BackgroundSelected` | `#0A200A` | Selected item |

### Text colours
| Key | Value | Role |
|-----|-------|------|
| `TextPrimary` | `#00FF41` | Bright phosphor green |
| `TextSecondary` | `#00C832` | Medium green |
| `TextTertiary` | `#008F11` | Dim green (hints, labels) |
| `TextDisabled` | `#004A00` | Very dim green |
| `TextAccent` | `#39FF14` | Neon green accent |

### Border colours
| Key | Value | Role |
|-----|-------|------|
| `BorderSubtle` | `#0F1F0F` | Barely visible separation |
| `BorderDefault` | `#1A301A` | Standard border |
| `BorderStrong` | `#2A4A2A` | Prominent border |
| `BorderAccent` | `#00FF41` | Active/focus indicator |

### Warning / Success brushes (per-theme, defined in Hacker.axaml)
| Key | Value |
|-----|-------|
| `BackgroundWarningBrush` | `#1A1500` |
| `TextWarningBrush` | `#FFFF00` |
| `BackgroundSuccessBrush` | `#001A00` |
| `TextSuccessBrush` | `#00FF41` |

### Status semantic brush overrides (override Base.axaml in Hacker.axaml)
These are added as `SolidColorBrush` entries that win over the ones in `Base.axaml` due to Avalonia's last-write-wins MergedDictionary semantics.
| Key | Value |
|-----|-------|
| `StatusSyncedBrush` | `#00FF41` |
| `StatusPendingBrush` | `#FFFF00` |
| `StatusConflictBrush` | `#FF4400` |
| `StatusExcludedBrush` | `#3A6A3A` |
| `StatusErrorBrush` | `#FF0040` |

### Rail active indicator
`RailActiveIndicatorBrush` → `Color="{StaticResource BorderAccent}"` (mirrors Light.axaml — do NOT hardcode hex)

---

## Files to Create

| File | Purpose |
|------|---------|
| `Themes/Hacker.axaml` | Full theme ResourceDictionary (mirrors Light.axaml structure — must include both `Color` resources AND their corresponding `SolidColorBrush` entries, e.g. `BackgroundPrimaryBrush Color="{StaticResource BackgroundPrimary}"`) |
| `Settings/ThemeOptionFactory.cs` | Static factory (mirrors `ConflictPolicyOptionFactory`) — creates `IReadOnlyList<ThemeOption>` using `ILocalizationService`. `ThemeOption` has two fields (`Theme`, `Label`) — no `Description`. Add XML doc on `Create`: `/// <summary>Creates the localised list of ThemeOption instances.</summary>` |

---

## Files to Modify

| File | Change |
|------|--------|
| `Infrastructure/Theme/AppTheme.cs` | Add `Hacker` enum value |
| `Infrastructure/Theme/ThemeService.cs` | Add `_hackerUri`; update `ApplyVariant` to switch on theme; extend existing-include LINQ predicate to include hacker URI |
| `Assets/Localization/en-GB.json` | Add `"Settings.Theme.Hacker": "Hacker"`; update description string |
| `Settings/SettingsViewModel.cs` | Replace `BuildThemeOptions()` call in constructor with `ThemeOptionFactory.Create(loc)`; replace `BuildThemeOptions()` call in `OnCultureChanged` with `ThemeOptionFactory.Create(loc)`; delete `BuildThemeOptions()` method |
| `Settings/SettingsView.axaml` | Replace 3 individual `<Button>` theme elements with `<ItemsControl ItemsSource="{Binding ThemeOptions}">` (mirrors PolicyOptions pattern); DataTemplate must specify `DataType="settings:ThemeOption"` (add `xmlns:settings` alias if not present); single `Click="OnThemeClick"` with `Tag="{Binding Theme}"`. Update description text |
| `Settings/SettingsView.axaml.cs` | Remove `OnThemeLightClick`, `OnThemeDarkClick`, `OnThemeSystemClick`; add single `OnThemeClick` reading `Button.Tag` as `AppTheme` |
| `Tests.Unit/Settings/GivenASettingsViewModel.cs` | Update `when_constructed_then_theme_options_contains_exactly_three_entries` → `.ShouldBe(4)`; update `when_culture_changed_then_theme_options_is_rebuilt` to also assert `Settings.Theme.Hacker` was called |

---

## ThemeService Change Detail

`ApplyVariant` currently does `resolved == AppTheme.Dark ? _darkUri : _lightUri`.

Change to a switch expression:

```csharp
private static readonly Uri _hackerUri = new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Hacker.axaml");

private static Uri ResolveUri(AppTheme resolved) => resolved switch
{
    AppTheme.Dark   => _darkUri,
    AppTheme.Hacker => _hackerUri,
    _               => _lightUri,
};
```

Extend the existing-include LINQ predicate:

```csharp
var existing = merged
    .OfType<ResourceInclude>()
    .FirstOrDefault(r => r.Source == _lightUri || r.Source == _darkUri || r.Source == _hackerUri);
```

---

## Localisation Changes

`en-GB.json`:
```json
"Settings.Theme.Hacker": "Hacker",
```

`SettingsView.axaml` subtitle currently reads `"Choose light, dark or follow the system setting."` — update literal to `"Choose a colour theme for the application."`.

---

## TDD Sequence (mandatory — commit RED before GREEN)

### Test project: `AStar.Dev.OneDrive.Sync.Client.Tests.Unit`

New test class `GivenAThemeOptionFactory` in `Tests.Unit/Settings/`:
- `when_create_called_then_returns_four_options`
- `when_create_called_then_all_theme_enum_values_are_covered`
- `when_create_called_then_hacker_uses_correct_localisation_key` (assert `Settings.Theme.Hacker` called on loc)

New tests in `GivenAThemeService` (mirrors existing pattern — test `CurrentTheme` state only; `ApplyVariant` calls `Application.Current` which is null in unit tests and returns early, so URI-loading cannot be asserted at this level):
- `when_apply_called_with_hacker_theme_then_current_theme_is_hacker`
- Add `AppTheme.Hacker` to the existing `[Theory]` `when_apply_called_with_any_theme_then_event_is_raised`

New tests in existing `GivenASettingsViewModel`:
- `when_building_theme_options_then_hacker_uses_correct_key`
- `when_culture_changed_then_theme_options_rebuild_includes_hacker_key`

Updates to existing tests in `GivenASettingsViewModel`:
- `when_constructed_then_theme_options_contains_exactly_three_entries` → rename to `when_constructed_then_theme_options_contains_exactly_four_entries`; change `.ShouldBe(3)` to `.ShouldBe(4)`
- `when_constructed_then_theme_options_covers_light_dark_and_system` → rename to `when_constructed_then_theme_options_covers_light_dark_system_and_hacker`; add `AppTheme.Hacker` to the assertion (do NOT add a new test — updating this one avoids overlapping assertions)
- `when_culture_changed_then_theme_options_is_rebuilt` → add assertion that `Settings.Theme.Hacker` was called

New tests for serialisation:
- `when_hacker_serialized_then_round_trips_correctly` — JSON serialize + deserialize `AppSettings { Theme = AppTheme.Hacker }` via `SettingsService` JSON options. Note: `SettingsService.JsonOpts` has no `JsonStringEnumConverter`; `AppTheme` serializes as integer. `Hacker` must be defined last in the enum (value `3`) so existing `settings.json` files with values 0/1/2 are unaffected.

Commit failing tests before touching production code.

---

## Implementation Order

1. Write failing tests → commit
2. `AppTheme.cs` — add `Hacker`
3. `Themes/Hacker.axaml` — full file
4. `ThemeService.cs` — add URI + update `ApplyVariant` and include-detection predicate
5. `en-GB.json` — add `Settings.Theme.Hacker`
6. `Settings/ThemeOptionFactory.cs` — new static factory
7. `SettingsViewModel.cs` — swap `ThemeOptions` initialiser + `CultureChanged` subscription
8. `SettingsView.axaml` — refactor to `ItemsControl`; update description text
9. `SettingsView.axaml.cs` — replace 3 handlers with 1 generic handler

---

## Verification

```bash
dotnet build apps/desktop/AStar.Dev.OneDrive.Sync.Client
dotnet test apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit
```

Zero errors, zero warnings. All existing tests green. New tests green.

Manual: launch app → Settings → click "Hacker" → full UI switches to phosphor-green-on-black. Switch back to Light/Dark/System → correct theme. Restart → Hacker persists from `settings.json`.

---

## Edge Cases Addressed

| Risk | Mitigation |
|------|-----------|
| `ApplyVariant` falls through to Light for unknown themes | `_` default → Light; Hacker explicitly handled before default |
| Existing-include cleanup misses Hacker URI → duplicate dictionaries | Hacker URI added to the `FirstOrDefault` predicate |
| System mode starts watcher for Hacker theme | System mode only resolves Dark/Light; Hacker is fixed — `ApplyVariantsOnUiThread` only calls `WatchSystem` when `theme == AppTheme.System` |
| New enum value breaks JSON round-trip | `SettingsService.JsonOpts` has no `JsonStringEnumConverter` — `AppTheme` serializes as integer. Appending `Hacker` at the end gives value `3`; existing files with 0/1/2 are unaffected. Covered by serialisation test. |
| Account accent colours (Base.axaml) on very dark background | Saturated accents readable on `#0A0A0A`; flagged for post-ship review if user wants full green-only accents |
| Hardcoded `Foreground="White"` on Save button (SettingsView.axaml:222) | Pre-existing issue outside this scope — not changed |
| Loc labels not updating on `CultureChanged` for `ThemeOptions` | Fixed by subscribing in `SettingsViewModel` constructor (same pattern as `PolicyOptions`) |
