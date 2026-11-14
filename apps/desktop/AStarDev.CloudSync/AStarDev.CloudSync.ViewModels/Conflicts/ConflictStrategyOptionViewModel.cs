using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One strategy button of a conflict: enabled, or disabled with the reason.</summary>
public sealed class ConflictStrategyOptionViewModel : LocalizedObservableObject
{
    private readonly Option<ResolutionNotAllowed> notAllowed;
    private readonly string path;

    internal ConflictStrategyOptionViewModel(ConflictStrategy strategy, Option<ResolutionNotAllowed> notAllowed, string path, IAsyncRelayCommand command, ILocalizationService localization) : base(localization)
    {
        Strategy = strategy;
        this.notAllowed = notAllowed;
        this.path = path;
        Command = command;
    }

    /// <summary>The strategy the button applies.</summary>
    public ConflictStrategy Strategy { get; }

    /// <summary>The button text.</summary>
    public string Text => Localization.GetLocal(ConflictTexts.StrategyKey(Strategy));

    /// <summary>The accessible name, which names the file too.</summary>
    public string AutomationName => Localization.GetLocal("Conflict.Action.AutomationName", Text, path);

    /// <summary>Whether the strategy applies to the conflict.</summary>
    public bool IsAllowed => notAllowed.Match(_ => false, () => true);

    /// <summary>Why the strategy does not apply; empty when it does.</summary>
    public string DisabledReasonText => notAllowed.Match(reason => Localization.GetLocal(ConflictTexts.NotAllowedKey(reason)), () => string.Empty);

    /// <summary>Applies the strategy. It cannot run when the strategy does not apply or the conflict is being resolved.</summary>
    public IAsyncRelayCommand Command { get; }
}
