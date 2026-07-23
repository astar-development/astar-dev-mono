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

The dialog defines its own `Window.Resources` (fonts, radii, colours) so it renders identically regardless of the host app's own theme.
