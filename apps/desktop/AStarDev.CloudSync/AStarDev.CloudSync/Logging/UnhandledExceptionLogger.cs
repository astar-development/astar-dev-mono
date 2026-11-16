using System.Diagnostics;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Logging;

/// <summary>Logs an exception that nothing caught (#1803), so a crash leaves a record in the log file and in Application Insights. Only the exception type and the first CloudSync method on its stack are logged; never the message (LG-02).</summary>
/// <param name="logger">Where the exception is logged.</param>
public sealed class UnhandledExceptionLogger(ILogger<UnhandledExceptionLogger> logger)
{
    private const string OwnNamespace = "AStarDev.";

    /// <summary>Logs the exception at Error level.</summary>
    /// <param name="source">Which hook caught it, for example <c>AppDomain</c>.</param>
    /// <param name="exception">What was thrown.</param>
    public void Log(string source, Exception exception)
    {
        var thrownIn = FirstOwnMethod(exception);
        LogMessage.ExceptionCaught(logger, thrownIn.Length == 0 ? source : $"{source} at {thrownIn}", exception.ErrorType());
    }

    private static string FirstOwnMethod(Exception exception) =>
        new StackTrace(exception).GetFrames()
            .Select(frame => frame.GetMethod())
            .Where(method => method?.DeclaringType?.FullName?.StartsWith(OwnNamespace, StringComparison.Ordinal) is true)
            .Select(method => $"{method!.DeclaringType!.FullName}.{method.Name}")
            .FirstOrDefault() ?? string.Empty;
}
