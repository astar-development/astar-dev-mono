using Microsoft.Graph;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

public interface IGraphClientFactory
{
    /// <summary>Creates a <see cref="GraphServiceClient"/> authenticated using the supplied token factory.</summary>
    GraphServiceClient CreateClient(Func<CancellationToken, Task<string>> tokenFactory);
}
