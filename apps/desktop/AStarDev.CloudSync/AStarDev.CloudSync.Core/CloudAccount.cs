using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Core;

/// <summary>A connected cloud storage account.</summary>
/// <param name="Id">The account id.</param>
/// <param name="DisplayName">The account's display name.</param>
/// <param name="Email">The account's sign-in email.</param>
/// <param name="Provider">The provider the account belongs to.</param>
/// <param name="Storage">Cloud storage used and available.</param>
/// <param name="FolderCount">Folders being synced.</param>
/// <param name="FileCount">Files being synced.</param>
/// <param name="Status">The account's sync status.</param>
/// <param name="LastSync">When the account last finished a sync, if ever.</param>
public sealed record CloudAccount(AccountId Id, string DisplayName, string Email, CloudProvider Provider, StorageUsage Storage, int FolderCount, int FileCount, SyncStatus Status, Option<DateTimeOffset> LastSync);
