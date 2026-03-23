namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents the minimum logging levels configuration, providing a mechanism
///     to define default logging levels as well as overrides for specific namespaces or components.
/// </summary>
public sealed class MinimumLevel
{
    /// <summary>
    ///     Represents the default logging level configuration within the <see cref="MinimumLevel" /> class,
    ///     allowing the specification of a baseline logging level for the application.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    ///     Represents configuration overrides for specific logging sources or categories within
    ///     the <see cref="MinimumLevel" /> class, allowing custom logging levels to be defined
    ///     for particular components of the application.
    /// </summary>
    public Override Override { get; set; } = new();
}