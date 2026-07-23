using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;

namespace AStar.Dev.OneDrive.Sync.Client.Updates;

/// <summary>Adapts <see cref="ILocalizationService"/> to the shared update dialog's <see cref="IUpdateDialogTextProvider"/> contract.</summary>
public sealed class LocalizedUpdateDialogTextProvider : IUpdateDialogTextProvider, IDisposable
{
    private readonly ILocalizationService localization;

    public LocalizedUpdateDialogTextProvider(ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);

        this.localization = localization;
        this.localization.CultureChanged += OnCultureChanged;
    }

    /// <inheritdoc />
    public string Title => localization.GetLocal("Update.Title");

    /// <inheritdoc />
    public string ReleaseNotesLabel => localization.GetLocal("Update.ReleaseNotesLabel");

    /// <inheritdoc />
    public string RestartNowLabel => localization.GetLocal("Update.RestartNow");

    /// <inheritdoc />
    public string LaterLabel => localization.GetLocal("Update.Later");

    /// <inheritdoc />
    public string DownloadingLabel => localization.GetLocal("Update.Downloading");

    /// <inheritdoc />
    public string GetMessage(string version) => localization.GetLocal("Update.Message", version);

    /// <inheritdoc />
    public event EventHandler? TextChanged;

    private void OnCultureChanged(object? sender, CultureInfo culture) => TextChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>Unsubscribes from <see cref="ILocalizationService.CultureChanged"/>.</summary>
    public void Dispose() => localization.CultureChanged -= OnCultureChanged;
}
