using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <summary>
/// Loads sync rules for an account, resolves drive and folder IDs, and streams
/// remote delta items — populating a <see cref="RemoteEnumerationContext"/> for downstream processing.
/// </summary>
public interface IRemoteFolderEnumerator
{
    /// <summary>
    /// Loads rules and synced-item state into <paramref name="context"/>, then yields each
    /// discovered <see cref="DeltaItem"/> as it arrives from the Graph API.
    /// <para>
    /// <see cref="RemoteEnumerationContext.Rules"/>, <see cref="RemoteEnumerationContext.SyncedItems"/>,
    /// and <see cref="RemoteEnumerationContext.HadNoRules"/> are set before the first item is yielded.
    /// <see cref="RemoteEnumerationContext.SeenRemoteIds"/> is updated for each yielded item.
    /// </para>
    /// </summary>
    IAsyncEnumerable<DeltaItem> StreamAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, RemoteEnumerationContext context, Action<int>? onItemDiscovered = null, CancellationToken ct = default);
}
