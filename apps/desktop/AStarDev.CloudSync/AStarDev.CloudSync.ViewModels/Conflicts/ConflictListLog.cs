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

    [LoggerMessage(EventId = 5324, Level = LogLevel.Warning, Message = "A held deletion set was not settled: the account is busy, sync is paused or a step failed.")]
    public static partial void HeldNotSettled(ILogger logger);

    [LoggerMessage(EventId = 5325, Level = LogLevel.Error, Message = "Settling a held deletion set failed unexpectedly ({ErrorType}).")]
    public static partial void HeldThrew(ILogger logger, string errorType);
}
