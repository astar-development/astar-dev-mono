using AStarDev.CloudSync.TestHelpers.Common;

namespace AStarDev.CloudSync.Sync.TestsUnit;

internal static class LoggedText
{
    internal static IEnumerable<string> From(params RecordingLoggerProvider[] providers)
    {
        foreach (var logs in providers.Select(provider => provider.Logger))
        {
            foreach (var message in logs.Messages)
                yield return message;

            foreach (var exception in logs.Entries.Select(entry => entry.Exception).OfType<Exception>())
                yield return exception.ToString();

            foreach (var scope in logs.Scopes)
                foreach (var pair in scope)
                    yield return $"{pair.Key}={pair.Value}";

            foreach (var logged in logs.Events)
                foreach (var pair in logged.Values)
                    yield return $"{pair.Key}={pair.Value}";
        }
    }

    internal static IReadOnlyList<string> Personal(IEnumerable<string> logged, IReadOnlyList<string> personalValues) => [.. logged.Where(text => personalValues.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase)))];
}
