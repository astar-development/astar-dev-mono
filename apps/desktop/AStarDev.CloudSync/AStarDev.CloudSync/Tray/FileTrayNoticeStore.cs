using System.IO.Abstractions;
using AStarDev.CloudSync.ViewModels.Tray;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Tray;

/// <summary>Remembers that the "still running in the tray" notice was shown by a marker file: the file exists or it does not (#1409).</summary>
/// <param name="marker">The marker file.</param>
/// <param name="logger">Reports a marker that cannot be written, by exception type only.</param>
public sealed class FileTrayNoticeStore(IFileInfo marker, ILogger<FileTrayNoticeStore> logger) : ITrayNoticeStore
{
    /// <inheritdoc />
    public TrayNoticeState Read() => throw new NotImplementedException($"{marker}{logger}");

    /// <inheritdoc />
    public void MarkShown() => throw new NotImplementedException();
}
