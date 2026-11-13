using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.SyncPairs;

/// <summary>A sync pair as shown in folder tables and sync pair tiles.</summary>
public sealed class SyncPairViewModel : LocalizedObservableObject
{
    private readonly SyncPair pair;
    private readonly CloudAccount account;
    private readonly TimeProvider timeProvider;
    private readonly ILeasedSelectionRuleWriter ruleWriter;
    private bool failed;

    /// <summary>Creates the view model without a way to change the pair's direction (previews and tests that do not use it).</summary>
    /// <param name="pair">The sync pair.</param>
    /// <param name="account">The account holding the pair's cloud folder.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    /// <param name="timeProvider">Supplies the current time for relative times.</param>
    public SyncPairViewModel(SyncPair pair, CloudAccount account, ILocalizationService localization, TimeProvider timeProvider) : this(pair, account, NoSelectionRuleWriter.Instance, _ => Task.CompletedTask, localization, timeProvider)
    {
    }

    /// <summary>Creates the view model.</summary>
    /// <param name="pair">The sync pair.</param>
    /// <param name="account">The account holding the pair's cloud folder.</param>
    /// <param name="ruleWriter">Saves the new direction.</param>
    /// <param name="reload">Reloads the overview once the new direction is saved.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    /// <param name="timeProvider">Supplies the current time for relative times.</param>
    public SyncPairViewModel(SyncPair pair, CloudAccount account, ILeasedSelectionRuleWriter ruleWriter, Func<CancellationToken, Task> reload, ILocalizationService localization, TimeProvider timeProvider) : base(localization)
    {
        this.pair = pair;
        this.account = account;
        this.timeProvider = timeProvider;
        this.ruleWriter = ruleWriter;
        ChangeDirectionCommand = new AsyncRelayCommand(cancellationToken => ChangeDirectionAsync(reload, cancellationToken));
        Provider = new ProviderViewModel(account.Provider, localization);
        Status = new StatusViewModel(pair.Status, localization);
    }

    /// <summary>The sync pair id.</summary>
    public SyncPairId Id => pair.Id;

    /// <summary>The pair's display name.</summary>
    public string Name => pair.Name;

    /// <summary>The local folder's path relative to the account's sync root.</summary>
    public string LocalPath => pair.LocalFolder.Value ?? string.Empty;

    /// <summary>The cloud folder's path. It mirrors the local folder's relative path.</summary>
    public string RemotePath => "/" + pair.LocalFolder.Value;

    /// <summary>The id of the account holding the cloud folder.</summary>
    public AccountId AccountId => account.Id;

    /// <summary>The display name of the account holding the cloud folder.</summary>
    public string AccountName => account.DisplayName;

    /// <summary>The account's provider.</summary>
    public ProviderViewModel Provider { get; }

    /// <summary>The pair's sync status.</summary>
    public StatusViewModel Status { get; }

    /// <summary>Which way changes flow, e.g. "Upload only".</summary>
    public string DirectionLabel => Localization.GetLocal(DirectionKey);

    /// <summary>A symbol for the direction, e.g. "→".</summary>
    public string DirectionGlyph => Localization.GetLocal($"{DirectionKey}.Glyph");

    /// <summary>Moves the pair to its next direction: two-way, upload only, download only, then two-way again.</summary>
    public IAsyncRelayCommand ChangeDirectionCommand { get; }

    /// <summary>The text of the change direction action.</summary>
    public string ChangeDirectionText => Localization.GetLocal("SyncPair.ChangeDirection");

    /// <summary>The text explaining that the direction could not be changed; empty unless the last change failed.</summary>
    public string ErrorText => failed ? Localization.GetLocal("SyncPair.ChangeDirectionFailed") : string.Empty;

    /// <summary>Whether <see cref="ErrorText"/> has something to show.</summary>
    public bool HasError => failed;

    /// <summary>Sync progress from 0 to 1, as given by <see cref="StatusViewModel.Progress" />.</summary>
    public double Progress => Status.Progress;

    /// <summary>File count and last sync, e.g. "1,118 files · 4 min ago".</summary>
    public string SummaryText => Localization.GetLocal(pair.FileCount == 1 ? "SyncPair.Summary.One" : "SyncPair.Summary.Many", pair.FileCount.ToString("N0", Localization.CurrentCulture), RelativeTimeFormatter.Format(pair.LastSync, Localization, timeProvider));

    /// <summary>When the pair last synced, relative to now, or "Never".</summary>
    public string LastSyncText => RelativeTimeFormatter.Format(pair.LastSync, Localization, timeProvider);

    private async Task ChangeDirectionAsync(Func<CancellationToken, Task> reload, CancellationToken cancellationToken)
    {
        var change = SelectionChangeFactory.CreateChangeDirection(pair.Id, NextDirection());
        _ = await Try.RunAsync(() => ruleWriter.ApplyAsync(account.Id, [change], cancellationToken), cancellationToken)
            .MatchAsync(applied => applied.Match(_ => ReloadAsync(reload, cancellationToken), _ => Task.FromResult(SetFailed(true))), _ => Task.FromResult(SetFailed(true)));
    }

    private async Task<Unit> ReloadAsync(Func<CancellationToken, Task> reload, CancellationToken cancellationToken)
    {
        SetFailed(false);
        await reload(cancellationToken);

        return Unit.Instance;
    }

    private Unit SetFailed(bool value)
    {
        failed = value;
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorText));

        return Unit.Instance;
    }

    private SyncDirection NextDirection() => pair.Direction switch
    {
        SyncDirection.TwoWay => SyncDirectionFactory.CreateUploadOnly(),
        SyncDirection.UploadOnly => SyncDirectionFactory.CreateDownloadOnly(),
        _ => SyncDirectionFactory.CreateTwoWay(),
    };

    private string DirectionKey => pair.Direction switch
    {
        SyncDirection.UploadOnly => "Direction.UploadOnly",
        SyncDirection.DownloadOnly => "Direction.DownloadOnly",
        _ => "Direction.TwoWay"
    };
}
