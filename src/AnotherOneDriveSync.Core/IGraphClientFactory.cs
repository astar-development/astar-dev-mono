using Microsoft.Graph;

namespace AnotherOneDriveSync.Core;

public interface IGraphClientFactory
{
    Task<GraphServiceClient> CreateAsync();
}
