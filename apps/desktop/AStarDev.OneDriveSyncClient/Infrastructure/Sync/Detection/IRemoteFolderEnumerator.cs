using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Accounts;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;

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
    /// <para>
    /// <paramref name="onStageChanged"/> reports localisation-key stage markers (e.g. connecting to the
    /// drive, resolving a folder id) for phases that precede the first discovered item, closing the UI
    /// feedback gap between authentication and the first enumeration progress event.
    /// </para>
    /// </summary>
    IAsyncEnumerable<DeltaItem> StreamAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, RemoteEnumerationContext context, Action<int>? onItemDiscovered = null, Action<string>? onStageChanged = null, CancellationToken cancellationToken = default);
}
