namespace AStarDev.CloudSync.Sync;

/// <summary>Applies a <see cref="LogQuery"/> to a <see cref="LogLine"/>.</summary>
public static class LogQueryExtensions
{
    /// <summary>Whether the line is one the query asks for.</summary>
    /// <param name="query">The query.</param>
    /// <param name="line">The line.</param>
    public static bool Matches(this LogQuery query, LogLine line) =>
        line.Level >= query.MinimumLevel && AccountMatches(query, line) && TextMatches(query, line);

    private static bool AccountMatches(LogQuery query, LogLine line) =>
        query.AccountHash.Match(wanted => line.AccountHash.Match(actual => string.Equals(wanted, actual, StringComparison.Ordinal), () => false), () => true);

    private static bool TextMatches(LogQuery query, LogLine line) =>
        query.Text.Length == 0
        || line.Message.Contains(query.Text, StringComparison.OrdinalIgnoreCase)
        || line.Category.Contains(query.Text, StringComparison.OrdinalIgnoreCase);
}
