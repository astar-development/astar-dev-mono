using System.IO.Abstractions;
using AStarDev.CloudSync.ViewModels.Tray;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Tray;

/// <summary>Remembers that the "still running in the tray" notice was shown by a marker file: the file exists or it does not (#1409).</summary>
/// <param name="marker">The marker file.</param>
/// <param name="logger">Reports a marker that cannot be written, by exception type only.</param>
public sealed class FileTrayNoticeStore(IFileInfo marker, ILogger<FileTrayNoticeStore> logger) : ITrayNoticeStore
{
    private const string Location = nameof(FileTrayNoticeStore);

    /// <inheritdoc />
    public TrayNoticeState Read()
    {
        marker.Refresh();

        return marker.Exists ? TrayNoticeStateFactory.CreateShown() : TrayNoticeStateFactory.CreatePending();
    }

    /// <inheritdoc />
    public void MarkShown() =>
        _ = Try.Run(() =>
        {
            marker.Directory?.Create();
            marker.FileSystem.File.WriteAllText(marker.FullName, string.Empty);

            return Unit.Instance;
        }).Match(written => written, NotWritten);

    private Unit NotWritten(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Unit.Instance;
    }
}
