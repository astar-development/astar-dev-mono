using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Helpers;

internal static class LoggedEvents
{
    public static ILogger<T> CreateEnabledLogger<T>()
    {
        var logger = Substitute.For<ILogger<T>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return logger;
    }

    public static IReadOnlyDictionary<string, object?> ValuesOf(this ILogger logger, string eventName)
        => logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && ((EventId)call.GetArguments()[1]!).Name == eventName)
            .Select(call => (IReadOnlyDictionary<string, object?>)((IEnumerable<KeyValuePair<string, object?>>)call.GetArguments()[2]!).ToDictionary(pair => pair.Key, pair => pair.Value))
            .ShouldHaveSingleItem();

    public static IReadOnlyList<string> AllLoggedValues(this ILogger logger)
        => [.. logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .SelectMany(call => (IEnumerable<KeyValuePair<string, object?>>)call.GetArguments()[2]!)
            .Select(pair => pair.Value?.ToString() ?? string.Empty)];
}
