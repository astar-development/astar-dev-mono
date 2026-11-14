using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One conflict of the list, with its strategy buttons and the error of its last attempt.</summary>
public sealed class ConflictItemViewModel : LocalizedObservableObject
{
    private readonly SyncAttentionItem.Conflict conflict;
    private readonly Func<ConflictItemViewModel, ConflictStrategy, CancellationToken, Task> resolve;
    private int busy;
    private Option<string> errorKey;
    private readonly bool skipped;
    private bool isSelected;

    internal ConflictItemViewModel(SyncAttentionItem.Conflict conflict, IConflictResolutionPlanner planner, Func<ConflictItemViewModel, ConflictStrategy, CancellationToken, Task> resolve, Option<string> errorKey, bool skipped, bool isSelected, ILocalizationService localization) : base(localization)
    {
        this.conflict = conflict;
        this.resolve = resolve;
        this.errorKey = errorKey;
        this.skipped = skipped;
        this.isSelected = isSelected;
        Options =
        [
            CreateOption(planner, ConflictStrategyFactory.CreateLocalWins()),
            CreateOption(planner, ConflictStrategyFactory.CreateRemoteWins()),
            CreateOption(planner, ConflictStrategyFactory.CreateKeepBoth()),
            CreateOption(planner, ConflictStrategyFactory.CreateSkip())
        ];
    }

    /// <summary>The conflict's id.</summary>
    public ConflictId Id => conflict.Id;

    /// <summary>The account the conflict belongs to.</summary>
    public AccountId AccountId => conflict.AccountId;

    /// <summary>The conflicted path.</summary>
    public string PathText => conflict.Path.Value;

    /// <summary>The kind of the conflict, in words.</summary>
    public string KindText => Localization.GetLocal(ConflictTexts.KindKey(conflict.Kind));

    /// <summary>The kind of the conflict.</summary>
    public ConflictKind Kind => conflict.Kind;

    /// <summary>When the conflict was detected.</summary>
    public string DetectedText => Localization.GetLocal("Conflict.Detected", conflict.DetectedAt.UtcDateTime.ToString("g", Localization.CurrentCulture));

    /// <summary>Keep local, Keep cloud, Keep both and Skip.</summary>
    public IReadOnlyList<ConflictStrategyOptionViewModel> Options { get; }

    /// <summary>The reasons some strategies are not offered; empty when all are.</summary>
    public string UnavailableText => string.Join(' ', Options.Select(option => option.DisabledReasonText).Where(reason => reason.Length > 0).Distinct());

    /// <summary>The error of the last attempt; empty when there is none.</summary>
    public string ErrorText => errorKey.Match(Localization.GetLocal, () => string.Empty);

    /// <summary>Whether the last attempt failed.</summary>
    public bool HasError => errorKey.Match(_ => true, () => false);

    /// <summary>Whether the conflict is being resolved now.</summary>
    public bool IsBusy => Volatile.Read(ref busy) == 1;

    /// <summary>Whether the user skipped the conflict in this session.</summary>
    public bool IsSkipped => skipped;

    /// <summary>Whether the conflict is ticked for a bulk apply.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    /// <summary>The accessible name of the tick box, which names the file.</summary>
    public string SelectAutomationName => throw new NotImplementedException($"{PathText}");

    /// <summary>The text shown for a skipped conflict.</summary>
    public string SkippedText => Localization.GetLocal("Conflict.Skipped");

    internal void ShowError(string key)
    {
        errorKey = Option.Some(key);
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(HasError));
    }

    private ConflictStrategyOptionViewModel CreateOption(IConflictResolutionPlanner planner, ConflictStrategy strategy)
    {
        var notAllowed = planner.Plan(conflict, strategy).Match(_ => Option.None<ResolutionNotAllowed>(), reason => Option.Some(reason));
        var allowed = notAllowed.Match(_ => false, () => true);
        var command = new AsyncRelayCommand(cancellationToken => RunAsync(strategy, cancellationToken), () => allowed && !IsBusy);

        return new ConflictStrategyOptionViewModel(strategy, notAllowed, PathText, command, Localization);
    }

    private async Task RunAsync(ConflictStrategy strategy, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
            return;

        using var scope = new BusyScope(this);
        SetBusyChanged();
        await resolve(this, strategy, cancellationToken);
    }

    private void SetBusyChanged()
    {
        OnPropertyChanged(nameof(IsBusy));

        foreach (var option in Options)
            option.Command.NotifyCanExecuteChanged();
    }

    private sealed class BusyScope(ConflictItemViewModel owner) : IDisposable
    {
        public void Dispose()
        {
            Volatile.Write(ref owner.busy, 0);
            owner.SetBusyChanged();
        }
    }
}
