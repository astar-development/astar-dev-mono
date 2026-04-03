using Microsoft.Graph;

namespace AStar.Dev.OneDrive.Client.Infrastructure;

/// <summary>
///     Creates authenticated <see cref="GraphServiceClient"/> instances from an access token.
///     Abstracts client construction so callers can be tested without real Graph calls.
/// </summary>
public interface IGraphClientFactory
{
    /// <summary>
    ///     Returns a <see cref="GraphServiceClient"/> configured with the supplied bearer token.
    /// </summary>
    GraphServiceClient Create(string accessToken);
}
