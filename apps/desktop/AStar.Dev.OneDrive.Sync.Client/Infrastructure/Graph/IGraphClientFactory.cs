using Microsoft.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public interface IGraphClientFactory
{
    /// <summary>Creates a <see cref="GraphServiceClient"/> authenticated with the supplied bearer token.</summary>
    GraphServiceClient CreateClient(string accessToken);
}
