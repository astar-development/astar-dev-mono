using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     Provides an implementation of <see cref="ILoggerAstar{T}" /> that integrates application telemetry tracking
///     with structured logging for enhanced observability in A* applications.
/// </summary>
/// <typeparam name="TCategoryName">The category name for the logger, typically derived from the type being logged.</typeparam>
public sealed class AStarLogger<TCategoryName>(ILogger<TCategoryName> logger, Microsoft.ApplicationInsights.TelemetryClient telemetryClient) : ILoggerAstar<TCategoryName>
{
    /// <inheritdoc />
    public void LogPageView(string pageName)
    {
        ArgumentNullException.ThrowIfNull(pageName);
        LogMessage.NotFound(logger, "/missing-resource");

        telemetryClient.TrackPageView(pageName);
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => logger.BeginScope(state);

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => logger.Log(logLevel, eventId, state, exception, formatter);
}