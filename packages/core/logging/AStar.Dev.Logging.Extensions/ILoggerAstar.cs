using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     Defines an interface for a logging mechanism that extends the basic ILogger functionalities
///     by adding support for logging specific page view events.
/// </summary>
/// <typeparam name="T">The type representing the category name for the logger.</typeparam>
public interface ILoggerAstar<out T> : ILogger<T>
{
    /// <summary>
    /// </summary>
    /// <param name="pageName"></param>
    void LogPageView(string pageName);
}