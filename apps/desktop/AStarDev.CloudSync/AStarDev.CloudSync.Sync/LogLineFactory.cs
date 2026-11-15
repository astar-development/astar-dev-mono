using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Makes <see cref="LogLine"/>s.</summary>
public static class LogLineFactory
{
    /// <summary>Makes a line.</summary>
    /// <param name="time">When the entry was written.</param>
    /// <param name="level">The level.</param>
    /// <param name="accountHash">The hashed account id, or none.</param>
    /// <param name="category">The category.</param>
    /// <param name="message">The message.</param>
    public static LogLine Create(DateTimeOffset time, LogLevel level, Option<string> accountHash, string category, string message) => new(time, level, accountHash, category, message);
}
