using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>The question shown when Quit is chosen while a sync is running: quit and cancel it, or keep syncing (#1409).</summary>
public sealed class ConfirmQuitViewModel(ILocalizationService localization) : LocalizedObservableObject(localization), IConfirmQuitDialog
{
    private readonly Lock gate = new();
    private TaskCompletionSource<QuitChoice>? pending;

    /// <summary>Whether the question is showing.</summary>
    public bool IsOpen
    {
        get
        {
            lock (gate)
                return pending is not null;
        }
    }

    /// <summary>The dialog heading.</summary>
    public string TitleText => Localization.GetLocal("Quit.Title");

    /// <summary>The question.</summary>
    public string MessageText => Localization.GetLocal("Quit.Message");

    /// <summary>The label of the button that cancels the syncs and quits.</summary>
    public string QuitText => Localization.GetLocal("Quit.QuitAnyway");

    /// <summary>The label of the button that keeps syncing.</summary>
    public string StayText => Localization.GetLocal("Quit.Stay");

    /// <summary>Answers "quit".</summary>
    public IRelayCommand QuitCommand => field ??= new RelayCommand(() => Answer(QuitChoiceFactory.CreateQuitAnyway()));

    /// <summary>Answers "keep syncing".</summary>
    public IRelayCommand StayCommand => field ??= new RelayCommand(() => Answer(QuitChoiceFactory.CreateStayOpen()));

    /// <inheritdoc />
    public Task<QuitChoice> AskAsync(CancellationToken cancellationToken)
    {
        Task<QuitChoice> answer;
        bool opened;
        lock (gate)
        {
            opened = pending is null;
            pending ??= new TaskCompletionSource<QuitChoice>(TaskCreationOptions.RunContinuationsAsynchronously);
            answer = pending.Task;
        }

        if (opened)
        {
            OnPropertyChanged(nameof(IsOpen));
            _ = cancellationToken.Register(() => Answer(QuitChoiceFactory.CreateStayOpen()));
        }

        return answer;
    }

    private void Answer(QuitChoice choice)
    {
        TaskCompletionSource<QuitChoice>? answered;
        lock (gate)
        {
            answered = pending;
            pending = null;
        }

        if (answered is null)
            return;

        OnPropertyChanged(nameof(IsOpen));
        _ = answered.TrySetResult(choice);
    }
}
