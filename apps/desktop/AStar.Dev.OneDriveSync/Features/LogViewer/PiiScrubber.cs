using System.Text.RegularExpressions;

namespace AStar.Dev.OneDriveSync.Features.LogViewer;

internal static partial class PiiScrubber
{
    private const string EmailPlaceholder = "[email redacted]";

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();

    /// <summary>Replaces any email addresses in <paramref name="message"/> with <c>[email redacted]</c>.</summary>
    internal static string Scrub(string message) => EmailRegex().Replace(message, EmailPlaceholder);
}
