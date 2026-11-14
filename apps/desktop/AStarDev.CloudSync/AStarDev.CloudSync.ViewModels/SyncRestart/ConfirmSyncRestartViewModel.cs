using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.SyncRestart;

/// <summary>The SE-08 question shown when Sync Now is pressed while the account is syncing: cancel the current sync and start again, or keep it (#1402).</summary>
public sealed class ConfirmSyncRestartViewModel(ILocalizationService localization) : LocalizedObservableObject(localization), IConfirmSyncRestartDialog
{
    private readonly Lock gate = new();
    private TaskCompletionSource<SyncRestartChoice>? pending;

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
    public string TitleText => Localization.GetLocal("SyncRestart.Title");

    /// <summary>The question.</summary>
    public string MessageText => Localization.GetLocal("SyncRestart.Message");

    /// <summary>The label of the button that cancels the current sync and starts again.</summary>
    public string CancelAndRestartText => Localization.GetLocal("SyncRestart.CancelAndRestart");

    /// <summary>The label of the button that keeps the current sync.</summary>
    public string KeepText => Localization.GetLocal("SyncRestart.Keep");

    /// <summary>Answers "cancel the current sync and start again".</summary>
    public IRelayCommand CancelAndRestartCommand => field ??= new RelayCommand(() => Answer(SyncRestartChoiceFactory.CreateCancelAndRestart()));

    /// <summary>Answers "keep the current sync".</summary>
    public IRelayCommand KeepCommand => field ??= new RelayCommand(() => Answer(SyncRestartChoiceFactory.CreateKeepCurrent()));

    /// <inheritdoc />
    public Task<SyncRestartChoice> AskAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        Task<SyncRestartChoice> answer;
        bool opened;
        lock (gate)
        {
            opened = pending is null;
            pending ??= new TaskCompletionSource<SyncRestartChoice>(TaskCreationOptions.RunContinuationsAsynchronously);
            answer = pending.Task;
        }

        if (opened)
        {
            OnPropertyChanged(nameof(IsOpen));
            _ = cancellationToken.Register(() => Answer(SyncRestartChoiceFactory.CreateKeepCurrent()));
        }

        return answer;
    }

    private void Answer(SyncRestartChoice choice)
    {
        TaskCompletionSource<SyncRestartChoice>? answered;
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
