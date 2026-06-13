using System.Collections.Concurrent;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Microsoft.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>Resolves and caches the <see cref="DriveContext"/> (drive ID + root item ID) for each OneDrive account, so that the Graph API is called at most once per account per session.</summary>
internal sealed class DriveContextCache(IGraphClientFactory graphClientFactory)
{
    private readonly ConcurrentDictionary<string, DriveContext> cache = [];

    /// <summary>Returns the <see cref="GraphServiceClient"/> together with the resolved <see cref="DriveContext"/> for the given account. The drive context is fetched from the Graph API on the first call and cached for subsequent calls.</summary>
    internal async Task<Result<(GraphServiceClient Client, DriveContext Ctx), string>> ResolveAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct)
    {
        var client = graphClientFactory.CreateClient(tokenFactory);

        if(cache.TryGetValue(accountId, out var cached))
            return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Ok((client, cached));

        var drive = await client.Me.Drive.GetAsync(cancellationToken: ct).ConfigureAwait(false);

        if(drive?.Id is null)
            return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Error("Could not retrieve drive ID.");

        var driveId = new DriveId(drive.Id);
        var root = await client.Drives[driveId.Value].Root.GetAsync(cancellationToken: ct).ConfigureAwait(false);

        if(root?.Id is null)
            return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Error("Could not retrieve root item ID.");

        var driveContext = new DriveContext(driveId, root.Id);
        cache[accountId] = driveContext;

        return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Ok((client, driveContext));
    }

    /// <summary>Removes the cached drive context for the given account. Call after sign-out to prevent stale entries accumulating.</summary>
    internal void Evict(string accountId) => cache.TryRemove(accountId, out _);
}
