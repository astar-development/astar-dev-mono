namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Supplies the display text shown in the update-available dialog, decoupled from any specific localisation mechanism.</summary>
public interface IUpdateDialogTextProvider
{
    /// <summary>The dialog's title text.</summary>
    string Title { get; }

    /// <summary>The label shown above the release notes section.</summary>
    string ReleaseNotesLabel { get; }

    /// <summary>The label for the button that restarts the app to apply the update immediately.</summary>
    string RestartNowLabel { get; }

    /// <summary>The label for the button that dismisses the dialog without updating.</summary>
    string LaterLabel { get; }

    /// <summary>The label shown while the update is downloading.</summary>
    string DownloadingLabel { get; }

    /// <summary>Returns the body message describing the available update, formatted with <paramref name="version"/>.</summary>
    string GetMessage(string version);

    /// <summary>Raised when the underlying text source changes (e.g. a culture switch) so an open dialog can refresh.</summary>
    event EventHandler? TextChanged;
}
