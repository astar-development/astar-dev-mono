using AStar.Dev.Velopack.Publishing.Avalonia.Updates;

namespace AStar.Dev.Wallpaper.Scraper.Updates;

/// <summary>
///     Supplies the update-available dialog's display text as hardcoded English strings, mirroring
///     AStarDev.OneDriveSyncClient's localized copy. The Scraper has no localisation infrastructure
///     yet, so this is a plain, non-localised implementation of <see cref="IUpdateDialogTextProvider"/>.
/// </summary>
public sealed class PlainUpdateDialogTextProvider : IUpdateDialogTextProvider
{
    /// <inheritdoc />
    public string Title => "Update available";

    /// <inheritdoc />
    public string ReleaseNotesLabel => "What's new";

    /// <inheritdoc />
    public string RestartNowLabel => "Restart now";

    /// <inheritdoc />
    public string LaterLabel => "Later";

    /// <inheritdoc />
    public string DownloadingLabel => "Downloading...";

    /// <inheritdoc />
    public string GetMessage(string version) => $"Version {version} is ready to install.";

    /// <inheritdoc />
    public event EventHandler? TextChanged
    {
        add
        {
            // No-op: this implementation has no dynamic text, so the event will never fire.
        }
        remove
        {
            // No-op: this implementation has no dynamic text, so the event will never fire.
        }
    }
}
