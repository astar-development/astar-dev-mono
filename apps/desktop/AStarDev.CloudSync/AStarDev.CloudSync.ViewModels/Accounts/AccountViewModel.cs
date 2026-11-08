using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>A cloud account as shown in account lists and cards.</summary>
public sealed class AccountViewModel : LocalizedObservableObject
{
    private const int RecentActivityLimit = 3;

    private readonly CloudAccount account;
    private readonly TimeProvider timeProvider;

    /// <summary>Creates the view model, keeping the account's three newest activity entries.</summary>
    /// <param name="account">The account.</param>
    /// <param name="activity">Activity across every account; only this account's entries are kept.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    /// <param name="timeProvider">Supplies the current time for relative times.</param>
    public AccountViewModel(CloudAccount account, IEnumerable<ActivityEntry> activity, ILocalizationService localization, TimeProvider timeProvider) : base(localization)
    {
        this.account = account;
        this.timeProvider = timeProvider;
        Provider = new ProviderViewModel(account.Provider, localization);
        Status = new StatusViewModel(account.Status, localization);
        Storage = new StorageViewModel(account.Storage, localization);
        RecentActivity = [.. activity.Where(entry => entry.AccountId == account.Id).OrderByDescending(entry => entry.OccurredAt).Take(RecentActivityLimit).Select(entry => new ActivityItemViewModel(entry, Provider, localization, timeProvider))];
    }

    /// <summary>The account id.</summary>
    public AccountId Id => account.Id;

    /// <summary>The account's display name.</summary>
    public string Name => account.DisplayName;

    /// <summary>The account's sign-in email.</summary>
    public string Email => account.Email;

    /// <summary>The account's provider.</summary>
    public ProviderViewModel Provider { get; }

    /// <summary>The account's sync status.</summary>
    public StatusViewModel Status { get; }

    /// <summary>The account's storage usage.</summary>
    public StorageViewModel Storage { get; }

    /// <summary>The number of synced folders.</summary>
    public int FolderCount => account.FolderCount;

    /// <summary>The number of synced files.</summary>
    public int FileCount => account.FileCount;

    /// <summary>The number of open conflicts.</summary>
    public int ConflictCount => account.Status is SyncStatus.HasConflicts conflicts ? conflicts.Count : 0;

    /// <summary>The number of synced folders, formatted for the culture.</summary>
    public string FoldersText => account.FolderCount.ToString("N0", Localization.CurrentCulture);

    /// <summary>The number of synced files, formatted for the culture.</summary>
    public string FilesText => account.FileCount.ToString("N0", Localization.CurrentCulture);

    /// <summary>The number of open conflicts, formatted for the culture.</summary>
    public string ConflictsText => ConflictCount.ToString("N0", Localization.CurrentCulture);

    /// <summary>When the account last synced, relative to now, or "Never".</summary>
    public string LastSyncText => RelativeTimeFormatter.Format(account.LastSync, Localization, timeProvider);

    /// <summary>The account's newest activity, newest first.</summary>
    public IReadOnlyList<ActivityItemViewModel> RecentActivity { get; }
}
