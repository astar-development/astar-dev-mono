using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Makes <see cref="LogQuery"/>s.</summary>
public static class LogQueryFactory
{
    /// <summary>A query that matches every entry.</summary>
    public static LogQuery CreateEverything() => new(Option.None<string>(), LogLevel.Trace, string.Empty);

    /// <summary>Makes a query. Missing text is treated as empty.</summary>
    /// <param name="accountHash">The account to show, or none.</param>
    /// <param name="minimumLevel">The lowest level to show.</param>
    /// <param name="text">The text to find.</param>
    public static LogQuery Create(Option<string> accountHash, LogLevel minimumLevel, string? text) => new(accountHash, minimumLevel, text ?? string.Empty);
}
