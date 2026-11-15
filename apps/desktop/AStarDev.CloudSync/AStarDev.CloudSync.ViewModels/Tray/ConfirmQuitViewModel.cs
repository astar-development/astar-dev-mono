using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>The question shown when Quit is chosen while a sync is running: quit and cancel it, or keep syncing (#1409).</summary>
public sealed class ConfirmQuitViewModel(ILocalizationService localization) : LocalizedObservableObject(localization), IConfirmQuitDialog
{
    /// <summary>Whether the question is showing.</summary>
    public bool IsOpen => throw new NotImplementedException($"{nameof(IsOpen)}");

    /// <summary>The dialog heading.</summary>
    public string TitleText => throw new NotImplementedException($"{nameof(TitleText)}");

    /// <summary>The question.</summary>
    public string MessageText => throw new NotImplementedException($"{nameof(MessageText)}");

    /// <summary>The label of the button that cancels the syncs and quits.</summary>
    public string QuitText => throw new NotImplementedException($"{nameof(QuitText)}");

    /// <summary>The label of the button that keeps syncing.</summary>
    public string StayText => throw new NotImplementedException($"{nameof(StayText)}");

    /// <summary>Answers "quit".</summary>
    public IRelayCommand QuitCommand => throw new NotImplementedException($"{nameof(QuitCommand)}");

    /// <summary>Answers "keep syncing".</summary>
    public IRelayCommand StayCommand => throw new NotImplementedException($"{nameof(StayCommand)}");

    /// <inheritdoc />
    public Task<QuitChoice> AskAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(cancellationToken)}");
}
