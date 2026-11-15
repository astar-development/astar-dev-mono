using Avalonia.Input;

namespace AStarDev.CloudSync.Logging;

/// <summary>The key combination that opens the hidden log viewer (#1411): Ctrl+Shift+Alt+L.</summary>
public static class LogViewerHotkey
{
    private static readonly KeyGesture Gesture = new(Key.L, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt);

    /// <summary>Whether the key press is the viewer's hotkey.</summary>
    /// <param name="keyEvent">The key press.</param>
    public static bool Matches(KeyEventArgs keyEvent) => Gesture.Matches(keyEvent);
}
