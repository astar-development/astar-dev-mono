using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

internal static partial class OneDriveTransferLog
{
    [LoggerMessage(EventId = 5420, Level = LogLevel.Information, Message = "Uploaded {Bytes} bytes in {FragmentCount} fragments.")]
    public static partial void UploadCompleted(ILogger logger, long bytes, int fragmentCount);

    [LoggerMessage(EventId = 5421, Level = LogLevel.Warning, Message = "A remote call failed: HTTP {StatusCode}, Graph error {ErrorCode}.")]
    public static partial void CallFailed(ILogger logger, int statusCode, string errorCode);

    [LoggerMessage(EventId = 5422, Level = LogLevel.Information, Message = "An upload resumed at byte {Offset} (attempt {Attempt}).")]
    public static partial void UploadResumed(ILogger logger, long offset, int attempt);

    [LoggerMessage(EventId = 5423, Level = LogLevel.Information, Message = "The upload session was gone, so the upload started again.")]
    public static partial void SessionRestarted(ILogger logger);

    [LoggerMessage(EventId = 5424, Level = LogLevel.Information, Message = "The local file changed during the upload, so the session was cancelled and the file waits for the next pass.")]
    public static partial void SourceChanged(ILogger logger);

    [LoggerMessage(EventId = 5425, Level = LogLevel.Information, Message = "The upload was cancelled and its session deleted.")]
    public static partial void UploadCancelled(ILogger logger);

    [LoggerMessage(EventId = 5426, Level = LogLevel.Information, Message = "No access token for the transfer: {Outcome}.")]
    public static partial void TokenUnavailable(ILogger logger, string outcome);

    [LoggerMessage(EventId = 5427, Level = LogLevel.Information, Message = "The remote item to delete was already gone.")]
    public static partial void AlreadyGone(ILogger logger);
}
