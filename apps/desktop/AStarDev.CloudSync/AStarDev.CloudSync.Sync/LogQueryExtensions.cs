namespace AStarDev.CloudSync.Sync;

/// <summary>Applies a <see cref="LogQuery"/> to a <see cref="LogLine"/>.</summary>
public static class LogQueryExtensions
{
    /// <summary>Whether the line is one the query asks for.</summary>
    /// <param name="query">The query.</param>
    /// <param name="line">The line.</param>
    public static bool Matches(this LogQuery query, LogLine line) => throw new NotImplementedException($"{query}{line}");
}
