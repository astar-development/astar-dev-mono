using System;
using AStar.Dev.Conflict.Resolution.Domain;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Conflict.Resolution.Infrastructure;

/// <summary>Compile-time log message templates for conflict resolution (NF-00).</summary>
public static partial class ConflictResolutionLogMessage
{
    /// <summary>Logs that a new conflict was detected and persisted.</summary>
    [LoggerMessage(EventId = 2000, Level = LogLevel.Warning, Message = "Conflict detected for file {FilePath} (account {AccountId}): {ConflictType}. Persisted as conflict {ConflictId}.")]
    public static partial void ConflictDetected(ILogger logger, string filePath, Guid accountId, ConflictType conflictType, Guid conflictId);

    /// <summary>Logs that a conflict was resolved.</summary>
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Conflict {ConflictId} for file {FilePath} resolved with strategy {Strategy}.")]
    public static partial void ConflictResolved(ILogger logger, Guid conflictId, string filePath, ResolutionStrategy strategy);

    /// <summary>Logs a destructive file action before execution (NF-04).</summary>
    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Destructive conflict resolution: {Action} on {FilePath} (conflict {ConflictId}).")]
    public static partial void DestructiveResolutionPending(ILogger logger, string action, string filePath, Guid conflictId);

    /// <summary>Logs that a cascade resolution was applied to a matching conflict.</summary>
    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Cascade resolution applied to conflict {ConflictId} for file {FilePath} with strategy {Strategy}.")]
    public static partial void CascadeResolutionApplied(ILogger logger, Guid conflictId, string filePath, ResolutionStrategy strategy);

    /// <summary>Logs that a conflict file rename was performed for KeepBoth.</summary>
    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Conflict copy renamed to {RenamedPath} for conflict {ConflictId}.")]
    public static partial void ConflictCopyRenamed(ILogger logger, string renamedPath, Guid conflictId);
}
