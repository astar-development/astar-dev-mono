namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents a configuration override for specific logging domains or namespaces.
/// </summary>
public sealed class Override
{
    /// <summary>
    ///     The Microsoft.AspNetCore logging level override configuration.
    /// </summary>
    public string MicrosoftAspNetCore { get; set; } = string.Empty;

    /// <summary>
    ///     The System.Net.Http logging level override configuration.
    /// </summary>
    public string SystemNetHttp { get; set; } = string.Empty;

    /// <summary>
    ///     The logging level override configuration specific to the AStar namespace or domain.
    /// </summary>
    public string AStar { get; set; } = string.Empty;
}