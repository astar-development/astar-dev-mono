namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents the configuration arguments for logging components.
/// </summary>
public sealed class Args
{
    /// <summary>
    ///     Gets or sets the URL of the server used for logging purposes.
    /// </summary>
    /// <remarks>
    ///     The default value is an empty string if not explicitly set.
    ///     This property can be used to configure the target server address for logging connectivity.
    /// </remarks>
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string ServerUrl { get; set; } = string.Empty;
#pragma warning restore CA1056 // URI-like properties should not be strings
}
