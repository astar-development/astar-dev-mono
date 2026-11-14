using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
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
    private int busy;
    private bool isExpanded;
    private bool isDeferred;
    private Option<string> errorKey;

    internal HeldDeletionItemViewModel(SyncAttentionItem.HeldDeletions held, string accountName, string pairName, HeldDeletionActions actions, Option<string> errorKey, bool isDeferred, ILocalizationService localization) : base(localization)
    {
        this.held = held;
        this.accountName = accountName;
        this.pairName = pairName;
        this.errorKey = errorKey;
        this.isDeferred = isDeferred;
        ConfirmCommand = new AsyncRelayCommand(cancellationToken => RunAsync(actions.Confirm, cancellationToken), () => !IsBusy);
        RestoreCommand = new AsyncRelayCommand(cancellationToken => RunAsync(actions.Restore, cancellationToken), () => !IsBusy);
        LaterCommand = new RelayCommand(() => actions.DecideLater(this));
        ToggleCommand = new RelayCommand(Toggle);
    }

    /// <summary>The held set's id.</summary>
    public HeldDeletionId Id => held.Id;

    /// <summary>The account the set belongs to.</summary>
    public AccountId AccountId => held.AccountId;

    /// <summary>The account and the sync pair.</summary>
    public string HeaderText => Localization.GetLocal("Held.Header", accountName, pairName);

    /// <summary>What would be deleted and where, with the count.</summary>
    public string SummaryText => Localization.GetLocal(CountKey.For(held.Direction is DeletionDirection.DeleteLocal ? "Held.DeleteLocal" : "Held.DeleteRemote", held.Paths.Count), held.Paths.Count.ToString("N0", Localization.CurrentCulture));

    /// <summary>The held paths; empty until the set is expanded, so a large set costs nothing while collapsed.</summary>
    public IReadOnlyList<string> PathTexts => isExpanded ? [.. held.Paths.Select(path => path.Value)] : [];

    /// <summary>Whether the paths are shown.</summary>
    public bool IsExpanded => isExpanded;

    /// <summary>The text of the show or hide button.</summary>
    public string ToggleText => Localization.GetLocal(isExpanded ? "Held.Hide" : "Held.Show");

    /// <summary>The accessible name of the show or hide button.</summary>
    public string ToggleAutomationName => Localization.GetLocal("Conflict.Action.AutomationName", ToggleText, HeaderText);

    /// <summary>Shows or hides the paths.</summary>
    public IRelayCommand ToggleCommand { get; }

    /// <summary>The text of the Confirm button.</summary>
    public string ConfirmText => Localization.GetLocal("Held.Confirm");

    /// <summary>The text of the Restore button.</summary>
    public string RestoreText => Localization.GetLocal("Held.Restore");

    /// <summary>The text of the Decide later button.</summary>
    public string LaterText => Localization.GetLocal("Held.Later");

    /// <summary>The accessible name of the Confirm button.</summary>
    public string ConfirmAutomationName => Localization.GetLocal("Conflict.Action.AutomationName", ConfirmText, HeaderText);

    /// <summary>The accessible name of the Restore button.</summary>
    public string RestoreAutomationName => Localization.GetLocal("Conflict.Action.AutomationName", RestoreText, HeaderText);

    /// <summary>The accessible name of the Decide later button.</summary>
    public string LaterAutomationName => Localization.GetLocal("Conflict.Action.AutomationName", LaterText, HeaderText);

    /// <summary>Runs the held deletions. It cannot run while the set is being settled.</summary>
    public IAsyncRelayCommand ConfirmCommand { get; }

    /// <summary>Undoes the held deletions. It cannot run while the set is being settled.</summary>
    public IAsyncRelayCommand RestoreCommand { get; }

    /// <summary>Keeps holding the deletions.</summary>
    public IRelayCommand LaterCommand { get; }

    /// <summary>Whether the set is being settled now.</summary>
    public bool IsBusy => Volatile.Read(ref busy) == 1;

    /// <summary>Whether the user chose Decide later in this session.</summary>
    public bool IsDeferred => isDeferred;

    /// <summary>The text shown for a set held for now.</summary>
    public string DeferredText => Localization.GetLocal("Held.Deferred");

    /// <summary>The error of the last attempt; empty when there is none.</summary>
    public string ErrorText => errorKey.Match(Localization.GetLocal, () => string.Empty);

    /// <summary>Whether the last attempt failed.</summary>
    public bool HasError => errorKey.Match(_ => true, () => false);

    internal void ShowError(string key)
    {
        errorKey = Option.Some(key);
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(HasError));
    }

    internal void MarkDeferred()
    {
        isDeferred = true;
        OnPropertyChanged(nameof(IsDeferred));
    }

    private void Toggle()
    {
        isExpanded = !isExpanded;
        OnPropertyChanged(nameof(IsExpanded));
        OnPropertyChanged(nameof(PathTexts));
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(ToggleAutomationName));
    }

    private async Task RunAsync(Func<HeldDeletionItemViewModel, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
            return;

        using var scope = new BusyScope(this);
        SetBusyChanged();
        await action(this, cancellationToken);
    }

    private void SetBusyChanged()
    {
        OnPropertyChanged(nameof(IsBusy));
        ConfirmCommand.NotifyCanExecuteChanged();
        RestoreCommand.NotifyCanExecuteChanged();
    }

    private sealed class BusyScope(HeldDeletionItemViewModel owner) : IDisposable
    {
        public void Dispose()
        {
            Volatile.Write(ref owner.busy, 0);
            owner.SetBusyChanged();
        }
    }
}
