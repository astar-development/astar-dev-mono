using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

public interface ITokenCacheService
{
    /// <summary>
    /// Registers the file-backed cache with the given
    /// <see cref="IPublicClientApplication"/> instance.
    /// Must be called once after the app is built.
    /// </summary>
    Task RegisterAsync(IPublicClientApplication app);

    string CacheDirectory { get; }
}
