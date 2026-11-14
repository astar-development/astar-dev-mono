using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One strategy button of a conflict: enabled, or disabled with the reason.</summary>
public sealed class ConflictStrategyOptionViewModel : LocalizedObservableObject
{
    internal ConflictStrategyOptionViewModel(ConflictStrategy strategy, Option<ResolutionNotAllowed> notAllowed, string path, IAsyncRelayCommand command, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{strategy}{notAllowed}{path}{command}");

    /// <summary>The strategy the button applies.</summary>
    public ConflictStrategy Strategy => throw new NotImplementedException(nameof(Strategy));

    /// <summary>The button text.</summary>
    public string Text => throw new NotImplementedException(nameof(Text));

    /// <summary>The accessible name, which names the file too.</summary>
    public string AutomationName => throw new NotImplementedException(nameof(AutomationName));

    /// <summary>Whether the strategy applies to the conflict.</summary>
    public bool IsAllowed => throw new NotImplementedException(nameof(IsAllowed));

    /// <summary>Why the strategy does not apply; empty when it does.</summary>
    public string DisabledReasonText => throw new NotImplementedException(nameof(DisabledReasonText));

    /// <summary>Applies the strategy. It cannot run when the strategy does not apply or the conflict is being resolved.</summary>
    public IAsyncRelayCommand Command => throw new NotImplementedException(nameof(Command));
}
