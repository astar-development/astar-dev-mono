using Microsoft.Extensions.Logging;

namespace AStarDev.LoggingExtensions;

/// <summary>Defines an interface for a logging mechanism that extends the basic ILogger functionalities by adding support for logging specific page view events.</summary>
/// <typeparam name="T">The type representing the category name for the logger.</typeparam>
public interface ILoggerAstar<out T> : ILogger<T>
{
    /// <summary>Logs a page view event with the specified page name.</summary>
    /// <param name="pageName">The name of the page being viewed.</param>
    void LogPageView(string pageName);
}
