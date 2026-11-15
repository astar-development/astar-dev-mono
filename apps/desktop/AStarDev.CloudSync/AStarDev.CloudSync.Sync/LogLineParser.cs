using System.Globalization;
using System.Text.RegularExpressions;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the text lines <c>FileLoggerProvider</c> writes (#1411).</summary>
public static partial class LogLineParser
{
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

    [GeneratedRegex(@"^(?<time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(?<level>[A-Za-z]+)\](?: \[AccountIdHash=(?<hash>[^\]]+)\])? (?<category>[^:]+): (?<message>.*)$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex EntryPattern();

    /// <summary>Reads one text line.</summary>
    /// <param name="text">The line.</param>
    /// <returns>The entry it starts, or none when it is not the start of an entry (for example an exception's stack line).</returns>
    public static Option<LogLine> Parse(string text) =>
        EntryPattern().Match(text) is { Success: true } match
            && DateTimeOffset.TryParseExact(match.Groups["time"].Value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            && Enum.TryParse<LogLevel>(match.Groups["level"].Value, out var level)
            && Enum.IsDefined(level)
                ? Option.Some(LogLineFactory.Create(time, level, HashOf(match), match.Groups["category"].Value, match.Groups["message"].Value))
                : Option.None<LogLine>();

    private static Option<string> HashOf(Match match) => match.Groups["hash"].Success ? Option.Some(match.Groups["hash"].Value) : Option.None<string>();
}
