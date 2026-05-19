using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
///   Provides extension methods for logging common application lifecycle events, such as starting up and shutting down, with consistent formatting and event IDs.
/// </summary>
public static partial class ApplicationMessages
{
    /// <summary>
    ///     Logs an application starting-up.
    /// </summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="applicationName">The name of the application to log the starting up for.</param>
    [LoggerMessage(EventId = 210, Level = LogLevel.Information, Message = "{applicationName} starting up.")]
    public static partial void Starting(ILogger logger, string applicationName);

    /// <summary>
    ///     Logs an application stopping.
    /// </summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="applicationName">The name of the application to log the stopping for.</param>
    [LoggerMessage(EventId = 211, Level = LogLevel.Information, Message = "{applicationName} stopping.")]
    public static partial void Stopping(ILogger logger, string applicationName);

    /// <summary>
    ///     Logs an application stopping.
    /// </summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="applicationName">The name of the application to log the stopping for.</param>
    /// <param name="memoryUsage">The final memory usage of the application.</param>
    [LoggerMessage(EventId = 212, Level = LogLevel.Information, Message = "{applicationName} stopping. Final memory usage: {memoryUsage} MB.")]
    public static partial void Stopping(ILogger logger, string applicationName, string memoryUsage);

    /// <summary>
    ///     Logs a successful application start-up.
    /// </summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="applicationName">The name of the application to log the successful start-up for.</param>
    [LoggerMessage(EventId = 213, Level = LogLevel.Information, Message = "{applicationName} started successfully.")]
    public static partial void StartupSuccessful(ILogger logger, string applicationName);
}
