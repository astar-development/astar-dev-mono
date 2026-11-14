using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

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
    public AccountViewModel(CloudAccount account, IEnumerable<ActivityEntry> activity, ILocalizationService localization, TimeProvider timeProvider) : this(account, activity, NoAccountCommands.Instance, localization, timeProvider)
    {
    }

    /// <summary>Shows an account with its sign-in and remove actions.</summary>
    /// <param name="account">The account.</param>
    /// <param name="activity">The recent activity of every account.</param>
    /// <param name="commands">Runs sign-in and removal.</param>
    /// <param name="localization">Supplies the text.</param>
    /// <param name="timeProvider">Formats relative times.</param>
    public AccountViewModel(CloudAccount account, IEnumerable<ActivityEntry> activity, IAccountCommands commands, ILocalizationService localization, TimeProvider timeProvider) : base(localization)
    {
        this.account = account;
        SignInCommand = new AsyncRelayCommand(cancellationToken => commands.SignInAsync(this, cancellationToken));
        RemoveCommand = new AsyncRelayCommand(cancellationToken => commands.RemoveAsync(this, cancellationToken));
        this.timeProvider = timeProvider;
        Provider = new ProviderViewModel(account.Provider, localization);
        Status = new StatusViewModel(account.Status, account.SignIn, localization);
        Storage = new StorageViewModel(account.Storage, localization);
        ChangeSyncFolderCommand = new AsyncRelayCommand(cancellationToken => commands.ChangeSyncFolderAsync(this, cancellationToken), () => !Status.IsSyncing);
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

    /// <summary>Whether the account must sign in again; the layouts show the Sign in action.</summary>
    public bool NeedsSignIn => Status.IsSignInRequired;

    /// <summary>Whether the last run failed and the account does not need to sign in; the layouts then show their failure message.</summary>
    public bool HasFailure => Status.IsFailed;

    /// <summary>The failure message for layout A's account card; empty when there is no failure.</summary>
    public string LayoutAFailureText => Status.FailureMessage("A");

    /// <summary>The failure message for layout B's account detail; empty when there is no failure.</summary>
    public string LayoutBFailureText => Status.FailureMessage("B");

    /// <summary>The failure message for layout C's account strip; empty when there is no failure.</summary>
    public string LayoutCFailureText => Status.FailureMessage("C");

    /// <summary>The Sign in action's text.</summary>
    public string SignInText => Localization.GetLocal("Account.SignIn");

    /// <summary>The Remove action's text.</summary>
    public string RemoveText => Localization.GetLocal("Account.Remove");

    /// <summary>Whether the account has finished a sync before.</summary>
    public bool HasSynced => account.LastSync.Match(_ => true, () => false);

    /// <summary>The Sync folder action's text.</summary>
    public string ChangeSyncFolderText => Localization.GetLocal("Account.SyncFolder");

    /// <summary>Signs the account in again.</summary>
    public IAsyncRelayCommand SignInCommand { get; }

    /// <summary>Asks to remove the account.</summary>
    public IAsyncRelayCommand RemoveCommand { get; }

    /// <summary>Opens the sync folder dialog; not available while the account syncs.</summary>
    public IAsyncRelayCommand ChangeSyncFolderCommand { get; }
}
