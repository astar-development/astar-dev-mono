using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Logging;

/// <summary>Logs an exception that nothing caught (#1803), so a crash leaves a record in the log file and in Application Insights. Only the exception type and the first CloudSync method on its stack are logged; never the message (LG-02).</summary>
/// <param name="logger">Where the exception is logged.</param>
public sealed class UnhandledExceptionLogger(ILogger<UnhandledExceptionLogger> logger)
{
    /// <summary>Logs the exception at Error level.</summary>
    /// <param name="source">Which hook caught it, for example <c>AppDomain</c>.</param>
    /// <param name="exception">What was thrown.</param>
    public void Log(string source, Exception exception) => throw new NotImplementedException($"{logger}{source}{exception}");
}
