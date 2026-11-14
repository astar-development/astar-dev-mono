using AStarDev.CloudSync.Sync;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>One bulk strategy button: applies its strategy to every ticked conflict.</summary>
public sealed class ConflictBulkOptionViewModel : LocalizedObservableObject
{
    internal ConflictBulkOptionViewModel(ConflictStrategy strategy, IAsyncRelayCommand command, ILocalizationService localization) : base(localization)
    {
        Strategy = strategy;
        Command = command;
    }

    /// <summary>The strategy the button applies.</summary>
    public ConflictStrategy Strategy { get; }

    /// <summary>The button text.</summary>
    public string Text => throw new NotImplementedException($"{Strategy}");

    /// <summary>The accessible name.</summary>
    public string AutomationName => throw new NotImplementedException($"{Strategy}");

    /// <summary>Applies the strategy to the ticked conflicts.</summary>
    public IAsyncRelayCommand Command { get; }
}
