using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Logging;

public sealed class GivenTheOneDriveSyncClientMessageLevels
{
    public static TheoryData<string, LogLevel> ExpectedLevels() => new()
    {
        { nameof(OneDriveSyncClientMessages.SyncWorkerJobCancelledRequeued), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.DownloadThrottled), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.DownloadNetworkError), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.DownloadCancelledDuringBackoff), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.DownloadMoveRetrying), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.DownloadMoveExhausted), LogLevel.Warning },
        { nameof(OneDriveSyncClientMessages.DownloadRetriesExhausted), LogLevel.Warning },
        { nameof(OneDriveSyncClientMessages.UploadChunkThrottled), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.UploadChunkNetworkError), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.UploadSessionPreconditionFailed), LogLevel.Debug },
        { nameof(OneDriveSyncClientMessages.UploadRetriesExhausted), LogLevel.Warning },
        { nameof(OneDriveSyncClientMessages.ApplicationTerminatedUnexpectedly), LogLevel.Critical },
        { nameof(OneDriveSyncClientMessages.UnhandledApplicationException), LogLevel.Critical },
    };

    [Theory]
    [MemberData(nameof(ExpectedLevels))]
    public void when_the_template_is_logged_then_its_level_follows_the_impact(string templateName, LogLevel expectedLevel)
    {
        var logger = new TestLogger<GivenTheOneDriveSyncClientMessageLevels>();

        Log(templateName, logger);

        logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(expectedLevel);
    }

    private static void Log(string templateName, ILogger logger)
    {
        switch (templateName)
        {
            case nameof(OneDriveSyncClientMessages.SyncWorkerJobCancelledRequeued): OneDriveSyncClientMessages.SyncWorkerJobCancelledRequeued(logger, 1, "/a"); break;
            case nameof(OneDriveSyncClientMessages.DownloadThrottled): OneDriveSyncClientMessages.DownloadThrottled(logger, 1, 1, 5); break;
            case nameof(OneDriveSyncClientMessages.DownloadNetworkError): OneDriveSyncClientMessages.DownloadNetworkError(logger, 1, 1, 5); break;
            case nameof(OneDriveSyncClientMessages.DownloadCancelledDuringBackoff): OneDriveSyncClientMessages.DownloadCancelledDuringBackoff(logger, 1, 5); break;
            case nameof(OneDriveSyncClientMessages.DownloadMoveRetrying): OneDriveSyncClientMessages.DownloadMoveRetrying(logger, "/a", 1, 5); break;
            case nameof(OneDriveSyncClientMessages.DownloadMoveExhausted): OneDriveSyncClientMessages.DownloadMoveExhausted(logger, "/a", 5, "IOException"); break;
            case nameof(OneDriveSyncClientMessages.DownloadRetriesExhausted): OneDriveSyncClientMessages.DownloadRetriesExhausted(logger, "rate limited", 5); break;
            case nameof(OneDriveSyncClientMessages.UploadChunkThrottled): OneDriveSyncClientMessages.UploadChunkThrottled(logger, 0, 1, 1, 1, 5); break;
            case nameof(OneDriveSyncClientMessages.UploadChunkNetworkError): OneDriveSyncClientMessages.UploadChunkNetworkError(logger, 0, 1, 1, 1, 5); break;
            case nameof(OneDriveSyncClientMessages.UploadSessionPreconditionFailed): OneDriveSyncClientMessages.UploadSessionPreconditionFailed(logger, "/a", 1, 1, 5); break;
            case nameof(OneDriveSyncClientMessages.UploadRetriesExhausted): OneDriveSyncClientMessages.UploadRetriesExhausted(logger, "rate limited", 5); break;
            case nameof(OneDriveSyncClientMessages.ApplicationTerminatedUnexpectedly): OneDriveSyncClientMessages.ApplicationTerminatedUnexpectedly(logger, "System.Exception"); break;
            case nameof(OneDriveSyncClientMessages.UnhandledApplicationException): OneDriveSyncClientMessages.UnhandledApplicationException(logger, "System.Exception"); break;
            default: throw new ArgumentOutOfRangeException(nameof(templateName), templateName, "No invocation for this template.");
        }
    }
}
