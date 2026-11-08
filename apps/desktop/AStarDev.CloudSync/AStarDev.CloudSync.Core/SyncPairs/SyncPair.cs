using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Core.SyncPairs;

/// <summary>One local folder linked to one cloud folder.</summary>
/// <param name="Id">The sync pair id.</param>
/// <param name="AccountId">The account holding the cloud folder.</param>
/// <param name="Name">The sync pair's display name.</param>
/// <param name="LocalFolder">The local folder, under its provider's local root.</param>
/// <param name="RemotePath">The cloud folder's path.</param>
/// <param name="Direction">Which way changes flow.</param>
/// <param name="Status">The sync pair's sync status.</param>
/// <param name="FileCount">Files in the pair.</param>
/// <param name="LastSync">When the pair last finished a sync, if ever.</param>
public sealed record SyncPair(SyncPairId Id, AccountId AccountId, string Name, DirectoryInfo LocalFolder, string RemotePath, SyncDirection Direction, SyncStatus Status, int FileCount, Option<DateTimeOffset> LastSync);
