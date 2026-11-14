using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>The attention-queue screen (#1608): the open conflicts grouped by account, filtered by kind, resolved one at a time.</summary>
public sealed class ConflictListViewModel : LocalizedObservableObject
{
    /// <summary>Creates the screen.</summary>
    /// <param name="overviewSource">Supplies the accounts.</param>
    /// <param name="attentionReader">Reads each account's open items.</param>
    /// <param name="resolver">Resolves a conflict.</param>
    /// <param name="planner">Says which strategies apply to a conflict.</param>
    /// <param name="localization">Supplies the text.</param>
    /// <param name="logger">Reports reads and resolutions that failed, without paths.</param>
    public ConflictListViewModel(ISyncOverviewSource overviewSource, ISyncAttentionReader attentionReader, IConflictResolver resolver, IConflictResolutionPlanner planner, ILocalizationService localization, ILogger<ConflictListViewModel> logger) : base(localization) => throw new NotImplementedException($"{overviewSource}{attentionReader}{resolver}{planner}{logger}");

    /// <summary>Whether the screen is showing.</summary>
    public bool IsOpen => throw new NotImplementedException(nameof(IsOpen));

    /// <summary>How far the read has got.</summary>
    public SyncOverviewLoadState LoadState => throw new NotImplementedException(nameof(LoadState));

    /// <summary>The conflicts that pass the filter, grouped by account.</summary>
    public IReadOnlyList<ConflictAccountGroupViewModel> Groups => throw new NotImplementedException(nameof(Groups));

    /// <summary>Every kind, then each kind.</summary>
    public IReadOnlyList<ConflictKindFilterViewModel> Filters => throw new NotImplementedException(nameof(Filters));

    /// <summary>The kind shown.</summary>
    public ConflictKindFilterViewModel SelectedFilter
    {
        get => throw new NotImplementedException(nameof(SelectedFilter));
        set => throw new NotImplementedException($"{nameof(SelectedFilter)}{value}");
    }

    /// <summary>Whether the read succeeded and nothing is listed.</summary>
    public bool IsEmpty => throw new NotImplementedException(nameof(IsEmpty));

    /// <summary>The text shown when nothing is listed.</summary>
    public string EmptyText => throw new NotImplementedException(nameof(EmptyText));

    /// <summary>The text explaining that the queue could not be read; empty unless the read failed.</summary>
    public string LoadErrorText => throw new NotImplementedException(nameof(LoadErrorText));

    /// <summary>The screen heading.</summary>
    public string TitleText => throw new NotImplementedException(nameof(TitleText));

    /// <summary>The text of the button that opens the screen.</summary>
    public string ReviewText => throw new NotImplementedException(nameof(ReviewText));

    /// <summary>The text of the close button.</summary>
    public string CloseText => throw new NotImplementedException(nameof(CloseText));

    /// <summary>The text of the refresh button.</summary>
    public string RefreshText => throw new NotImplementedException(nameof(RefreshText));

    /// <summary>The label of the kind filter.</summary>
    public string FilterLabelText => throw new NotImplementedException(nameof(FilterLabelText));

    /// <summary>Shows the screen and reads the queue.</summary>
    public IAsyncRelayCommand OpenCommand => throw new NotImplementedException(nameof(OpenCommand));

    /// <summary>Hides the screen.</summary>
    public IRelayCommand CloseCommand => throw new NotImplementedException(nameof(CloseCommand));

    /// <summary>Reads the queue again.</summary>
    public IAsyncRelayCommand RefreshCommand => throw new NotImplementedException(nameof(RefreshCommand));

    /// <summary>Reads every account's open conflicts. A failed read is logged and shown as such, never as an empty queue; cancellation still throws.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public Task LoadAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(LoadAsync)}{cancellationToken}");
}
