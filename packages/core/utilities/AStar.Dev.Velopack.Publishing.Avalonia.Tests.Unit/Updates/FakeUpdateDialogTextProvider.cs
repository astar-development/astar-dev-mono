using AStar.Dev.Velopack.Publishing.Avalonia.Updates;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Tests.Unit.Updates;

internal sealed class FakeUpdateDialogTextProvider : IUpdateDialogTextProvider
{
    public string Title => "Update.Title";

    public string ReleaseNotesLabel => "Update.ReleaseNotesLabel";

    public string RestartNowLabel => "Update.RestartNow";

    public string LaterLabel => "Update.Later";

    public string DownloadingLabel => "Update.Downloading";

    public string GetMessage(string version) => $"Update.Message:{version}";

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
