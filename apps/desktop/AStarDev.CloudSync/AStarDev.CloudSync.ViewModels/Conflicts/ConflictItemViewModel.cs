using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One conflict of the list, with its strategy buttons and the error of its last attempt.</summary>
public sealed class ConflictItemViewModel : LocalizedObservableObject
{
    internal ConflictItemViewModel(SyncAttentionItem.Conflict conflict, IConflictResolutionPlanner planner, Func<ConflictItemViewModel, ConflictStrategy, CancellationToken, Task> resolve, Option<string> errorKey, bool skipped, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{conflict}{planner}{resolve}{errorKey}{skipped}");

    /// <summary>The conflict's id.</summary>
    public ConflictId Id => throw new NotImplementedException(nameof(Id));

    /// <summary>The account the conflict belongs to.</summary>
    public AccountId AccountId => throw new NotImplementedException(nameof(AccountId));

    /// <summary>The conflicted path.</summary>
    public string PathText => throw new NotImplementedException(nameof(PathText));

    /// <summary>The kind of the conflict, in words.</summary>
    public string KindText => throw new NotImplementedException(nameof(KindText));

    /// <summary>The kind of the conflict.</summary>
    public ConflictKind Kind => throw new NotImplementedException(nameof(Kind));

    /// <summary>When the conflict was detected.</summary>
    public string DetectedText => throw new NotImplementedException(nameof(DetectedText));

    /// <summary>Keep local, Keep cloud, Keep both and Skip.</summary>
    public IReadOnlyList<ConflictStrategyOptionViewModel> Options => throw new NotImplementedException(nameof(Options));

    /// <summary>The reasons some strategies are not offered; empty when all are.</summary>
    public string UnavailableText => throw new NotImplementedException(nameof(UnavailableText));

    /// <summary>The error of the last attempt; empty when there is none.</summary>
    public string ErrorText => throw new NotImplementedException(nameof(ErrorText));

    /// <summary>Whether the last attempt failed.</summary>
    public bool HasError => throw new NotImplementedException(nameof(HasError));

    /// <summary>Whether the conflict is being resolved now.</summary>
    public bool IsBusy => throw new NotImplementedException(nameof(IsBusy));

    /// <summary>Whether the user skipped the conflict in this session.</summary>
    public bool IsSkipped => throw new NotImplementedException(nameof(IsSkipped));

    /// <summary>The text shown for a skipped conflict.</summary>
    public string SkippedText => throw new NotImplementedException(nameof(SkippedText));
}
