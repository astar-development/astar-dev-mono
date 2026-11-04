using System.Globalization;
using AStarDev.Localization;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Adapts <see cref="ILocalizationService"/> to the shared update dialog's <see cref="IUpdateDialogTextProvider"/> contract.</summary>
public sealed class LocalizedUpdateDialogTextProvider : IUpdateDialogTextProvider, IDisposable
{
    private readonly ILocalizationService localization;
    private bool disposed;

    /// <summary>Creates a provider that reads the <c>Update.*</c> keys from <paramref name="localization"/> and raises <see cref="TextChanged"/> when its culture changes.</summary>
    /// <param name="localization">The localisation service supplying the dialog text.</param>
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
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            localization.CultureChanged -= OnCultureChanged;
    }
}
