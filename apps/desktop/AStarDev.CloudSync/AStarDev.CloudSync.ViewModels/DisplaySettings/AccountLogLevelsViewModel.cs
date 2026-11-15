using System.ComponentModel;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The per-account verbose logging boxes (#1411): one row for each account, kept in step with the accounts the overview shows.</summary>
public sealed class AccountLogLevelsViewModel : LocalizedObservableObject
{
    private readonly SyncOverviewViewModel overview;
    private readonly IAccountLogLevelSettings settings;
    private bool following;
    private string errorKey = string.Empty;

    /// <summary>Creates the setting; call <see cref="LoadAsync"/> to show the saved levels.</summary>
    /// <param name="overview">Supplies the accounts.</param>
    /// <param name="settings">Reads and saves the levels.</param>
    /// <param name="localization">Supplies the text.</param>
    public AccountLogLevelsViewModel(SyncOverviewViewModel overview, IAccountLogLevelSettings settings, ILocalizationService localization) : base(localization)
    {
        this.overview = overview;
        this.settings = settings;
    }

    /// <summary>One row for each account.</summary>
    public IReadOnlyList<AccountLogLevelRowViewModel> Rows { get; private set; } = [];

    /// <summary>The group heading.</summary>
    public string HeadingText => Localization.GetLocal("Settings.Logging");

    /// <summary>What verbose logging does and where it goes.</summary>
    public string HelpText => Localization.GetLocal("Settings.Logging.Help");

    /// <summary>Shown when there are no accounts yet.</summary>
    public string NoAccountsText => Localization.GetLocal("Settings.Logging.NoAccounts");

    /// <summary>Whether there is at least one account row.</summary>
    public bool HasAccounts => Rows.Count > 0;

    /// <summary>The message shown when the saved levels could not be read, or empty.</summary>
    public string ErrorText => errorKey.Length == 0 ? string.Empty : Localization.GetLocal(errorKey);

    /// <summary>Builds the rows from the accounts and their saved levels, and follows later changes to the accounts.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!following)
        {
            following = true;
            overview.PropertyChanged += OnOverviewChanged;
        }

        await RebuildAsync(cancellationToken);
    }

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyncOverviewViewModel.Accounts))
            _ = RebuildAsync(CancellationToken.None);
    }

    private async Task RebuildAsync(CancellationToken cancellationToken)
    {
        var unreadable = false;
        var rows = new List<AccountLogLevelRowViewModel>();
        foreach (var account in overview.Accounts)
        {
            var level = await settings.GetAsync(account.Id, cancellationToken).MatchAsync(found => found, _ =>
            {
                unreadable = true;

                return AccountLogLevelFactory.CreateStandard();
            });
            rows.Add(new AccountLogLevelRowViewModel(account.Id, account.Name, level, settings, Localization));
        }

        Rows = rows;
        errorKey = unreadable ? "Settings.Logging.LoadFailed" : string.Empty;
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(HasAccounts));
        OnPropertyChanged(nameof(ErrorText));
    }
}
