using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Activity;

/// <summary>The full activity list of an activity page or tab: the newest entries of one account or of every account, newest first, read each time the page is shown.</summary>
public sealed class ActivityListViewModel : LocalizedObservableObject
{
    private readonly IActivityLogReader reader;
    private readonly SyncOverviewViewModel overview;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ActivityListViewModel> logger;
    private bool hasError;

    /// <summary>Creates the list; it shows nothing until <see cref="RefreshCommand"/> runs.</summary>
    /// <param name="reader">Reads the activity log.</param>
    /// <param name="overview">Supplies the accounts the entries belong to.</param>
    /// <param name="timeProvider">Supplies the current time for relative times.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    /// <param name="logger">Reports a read that failed, without details.</param>
    public ActivityListViewModel(IActivityLogReader reader, SyncOverviewViewModel overview, TimeProvider timeProvider, ILocalizationService localization, ILogger<ActivityListViewModel> logger) : base(localization)
    {
        this.reader = reader;
        this.overview = overview;
        this.timeProvider = timeProvider;
        this.logger = logger;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>The only account whose activity is listed, or none for every account.</summary>
    internal Option<AccountId> AccountFilter { get; init; } = Option.None<AccountId>();

    /// <summary>The entries, newest first.</summary>
    public IReadOnlyList<ActivityItemViewModel> Items { get; private set; } = [];

    /// <summary>Whether the read worked and found nothing.</summary>
    public bool IsEmpty => Items.Count == 0 && !hasError;

    /// <summary>Whether the last read failed.</summary>
    public bool HasError => hasError;

    /// <summary>The list heading.</summary>
    public string TitleText => Localization.GetLocal("Activity.List.Title");

    /// <summary>Shown when there is no activity.</summary>
    public string EmptyText => Localization.GetLocal("Activity.List.Empty");

    /// <summary>Why the last read failed, or empty.</summary>
    public string ErrorText => hasError ? Localization.GetLocal("Activity.List.LoadFailed") : string.Empty;

    /// <summary>Reads the activity again.</summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var read = await reader.GetRecentAsync(AccountFilter, ActivityLogLimits.EntriesPerAccount, cancellationToken).ConfigureAwait(true);
        _ = read.Match(Show, NotRead);
    }

    private Unit Show(IReadOnlyList<ActivityEntry> entries)
    {
        var accountsById = overview.Accounts.ToDictionary(account => account.Id);
        Items = [.. entries.Where(entry => accountsById.ContainsKey(entry.AccountId)).Select(entry => new ActivityItemViewModel(entry, accountsById[entry.AccountId].Name, accountsById[entry.AccountId].Provider, Localization, timeProvider))];

        return Changed(false);
    }

    private Unit NotRead(ErrorResponse error)
    {
        ActivityListLog.ReadFailed(logger);
        Items = [];

        return Changed(true);
    }

    private Unit Changed(bool failed)
    {
        hasError = failed;
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorText));

        return Unit.Instance;
    }
}
