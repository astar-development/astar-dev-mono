using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the text lines <c>FileLoggerProvider</c> writes (#1411).</summary>
public static class LogLineParser
{
    /// <summary>Reads one text line.</summary>
    /// <param name="text">The line.</param>
    /// <returns>The entry it starts, or none when it is not the start of an entry (for example an exception's stack line).</returns>
    public static Option<LogLine> Parse(string text) => throw new NotImplementedException(text);
}
