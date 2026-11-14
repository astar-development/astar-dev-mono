using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

internal static partial class ConflictListLog
{
    [LoggerMessage(EventId = 5320, Level = LogLevel.Error, Message = "Could not read the conflict list ({ErrorType}).")]
    public static partial void ReadThrew(ILogger logger, string errorType);

    [LoggerMessage(EventId = 5321, Level = LogLevel.Error, Message = "Could not read the conflict list: a source reported a failure.")]
    public static partial void ReadFailed(ILogger logger);

    [LoggerMessage(EventId = 5322, Level = LogLevel.Error, Message = "Resolving a conflict failed unexpectedly ({ErrorType}).")]
    public static partial void ResolveThrew(ILogger logger, string errorType);

    [LoggerMessage(EventId = 5323, Level = LogLevel.Warning, Message = "A conflict resolution was refused: the account is busy or sync is paused.")]
    public static partial void ResolveRefused(ILogger logger);
}
