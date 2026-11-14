using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One set of held deletions of the list: the pair, the direction, the count and the paths, with Confirm, Restore and Decide later.</summary>
public sealed class HeldDeletionItemViewModel : LocalizedObservableObject
{
    private readonly SyncAttentionItem.HeldDeletions held;
    private readonly string accountName;
    private readonly string pairName;
    private readonly HeldDeletionActions actions;
    private int busy;
    private bool isExpanded;
    private bool isDeferred;
    private Option<string> errorKey;

    internal HeldDeletionItemViewModel(SyncAttentionItem.HeldDeletions held, string accountName, string pairName, HeldDeletionActions actions, Option<string> errorKey, bool isDeferred, ILocalizationService localization) : base(localization)
    {
        this.held = held;
        this.accountName = accountName;
        this.pairName = pairName;
        this.actions = actions;
        this.errorKey = errorKey;
        this.isDeferred = isDeferred;
        busy = 0;
        isExpanded = false;
    }

    /// <summary>The held set's id.</summary>
    public HeldDeletionId Id => held.Id;

    /// <summary>The account the set belongs to.</summary>
    public AccountId AccountId => held.AccountId;

    /// <summary>The account and the sync pair.</summary>
    public string HeaderText => throw new NotImplementedException($"{accountName}{pairName}{actions}");

    /// <summary>What would be deleted and where, with the count.</summary>
    public string SummaryText => throw new NotImplementedException($"{held}");

    /// <summary>The held paths.</summary>
    public IReadOnlyList<string> PathTexts => throw new NotImplementedException($"{held}");

    /// <summary>Whether the paths are shown.</summary>
    public bool IsExpanded => isExpanded;

    /// <summary>The text of the show or hide button.</summary>
    public string ToggleText => throw new NotImplementedException($"{isExpanded}");

    /// <summary>Shows or hides the paths.</summary>
    public IRelayCommand ToggleCommand => throw new NotImplementedException($"{busy}");

    /// <summary>The text of the Confirm button.</summary>
    public string ConfirmText => throw new NotImplementedException($"{busy}");

    /// <summary>The text of the Restore button.</summary>
    public string RestoreText => throw new NotImplementedException($"{busy}");

    /// <summary>The text of the Decide later button.</summary>
    public string LaterText => throw new NotImplementedException($"{busy}");

    /// <summary>The accessible name of the Confirm button.</summary>
    public string ConfirmAutomationName => throw new NotImplementedException($"{busy}");

    /// <summary>The accessible name of the Restore button.</summary>
    public string RestoreAutomationName => throw new NotImplementedException($"{busy}");

    /// <summary>The accessible name of the Decide later button.</summary>
    public string LaterAutomationName => throw new NotImplementedException($"{busy}");

    /// <summary>The accessible name of the show or hide button.</summary>
    public string ToggleAutomationName => throw new NotImplementedException($"{busy}");

    /// <summary>Runs the held deletions.</summary>
    public IAsyncRelayCommand ConfirmCommand => throw new NotImplementedException($"{busy}");

    /// <summary>Undoes the held deletions.</summary>
    public IAsyncRelayCommand RestoreCommand => throw new NotImplementedException($"{busy}");

    /// <summary>Keeps holding the deletions.</summary>
    public IRelayCommand LaterCommand => throw new NotImplementedException($"{busy}");

    /// <summary>Whether the set is being settled now.</summary>
    public bool IsBusy => Volatile.Read(ref busy) == 1;

    /// <summary>Whether the user chose Decide later in this session.</summary>
    public bool IsDeferred => isDeferred;

    /// <summary>The text shown for a set held for now.</summary>
    public string DeferredText => throw new NotImplementedException($"{busy}");

    /// <summary>The error of the last attempt; empty when there is none.</summary>
    public string ErrorText => errorKey.Match(Localization.GetLocal, () => string.Empty);

    /// <summary>Whether the last attempt failed.</summary>
    public bool HasError => errorKey.Match(_ => true, () => false);

    internal void ShowError(string key) => throw new NotImplementedException($"{key}");

    internal void MarkDeferred() => throw new NotImplementedException($"{isDeferred}");
}
