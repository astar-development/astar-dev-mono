using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Accounts;

internal static partial class AddAccountDialogLog
{
    [LoggerMessage(EventId = 5310, Level = LogLevel.Error, Message = "The add-account attempt failed unexpectedly ({ErrorType}).")]
    public static partial void AttemptFailedUnexpectedly(ILogger logger, string errorType);
}
