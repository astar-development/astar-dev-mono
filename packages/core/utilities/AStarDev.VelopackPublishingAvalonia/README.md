# AStar.Dev.Velopack.Publishing.Avalonia

Shared Avalonia update-available dialog, view model, and notification services used by AStar Development desktop apps, built on top of `AStar.Dev.Velopack.Publishing`.

## Usage

```csharp
services.AddVelopackUpdates(configuration);
services.AddVelopackUpdateNotifications();
services.AddSingleton<IUpdateDialogTextProvider, MyUpdateDialogTextProvider>();
```

`AddVelopackUpdateNotifications` registers `IUpdateAvailableDialogService`, `IUpdateAvailableViewModelFactory`, and `IUpdateNotificationService`. Callers must separately register an `IUpdateDialogTextProvider` implementation supplying the dialog's display text (title, release-notes label, button labels, downloading label, and the versioned message), decoupled from any specific localisation mechanism.

Call `IUpdateNotificationService.CheckAndNotifyAsync()` once the main window is available. If an update is found, the shared `UpdateAvailableView` dialog is shown with title/version/release notes, a visible downloading state, and a themed Restart-now/Later flow.

The dialog follows the host app's own theme. It only defines non-theme-dependent constants locally (`FontSizeSm`, `FontSizeXl`, `RadiusMd`); colours resolve via `DynamicResource` up to the host app's `Application`-scope resources, so the host app must define these 8 keys, ideally as a `ResourceDictionary.ThemeDictionaries` with `Light`/`Dark` entries so they follow `ThemeVariant` automatically:

- `BackgroundPrimaryBrush`
- `TextPrimaryBrush`
- `TextSecondaryBrush`
- `TextTertiaryBrush`
- `TextAccentBrush`
- `BorderSubtleBrush`
- `BorderDefaultBrush`
- `StatusErrorBrush`

See `AStarDev.OneDriveSyncClient/Themes/Light.axaml`/`Dark.axaml`/`Hacker.axaml` or `AStar.Dev.Wallpaper.Scraper/Theming/UpdateDialogPalette.axaml` for examples.
