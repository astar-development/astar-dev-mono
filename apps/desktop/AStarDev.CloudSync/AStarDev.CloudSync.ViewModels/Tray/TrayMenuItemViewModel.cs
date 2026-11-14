using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>One entry of the tray icon's menu (#1409).</summary>
/// <param name="text">The localised label.</param>
/// <param name="command">What choosing the entry does.</param>
public sealed class TrayMenuItemViewModel(string text, IAsyncRelayCommand command)
{
    /// <summary>The label.</summary>
    public string Text { get; } = text;

    /// <summary>Runs when the entry is chosen.</summary>
    public IAsyncRelayCommand Command { get; } = command;
}
