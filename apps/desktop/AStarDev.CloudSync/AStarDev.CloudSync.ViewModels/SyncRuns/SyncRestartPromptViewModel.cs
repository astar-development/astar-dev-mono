using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.SyncRuns;

/// <summary>The question shown when Sync Now is pressed while a sync is already running (SE-08, #1402): cancel it and start again, or keep it.</summary>
/// <param name="dispatcher">Runs the UI updates on the UI thread.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class SyncRestartPromptViewModel(IUiDispatcher dispatcher, ILocalizationService localization) : LocalizedObservableObject(localization), ISyncRestartPrompt
{
    private Option<TaskCompletionSource<SyncRestartChoice>> pending = Option.None<TaskCompletionSource<SyncRestartChoice>>();

    /// <summary>Whether the question is showing.</summary>
    public bool IsAsking => pending.Match(_ => true, () => false);

    /// <summary>The heading.</summary>
    public string TitleText => Localization.GetLocal("SyncRestart.Title");

    /// <summary>The question.</summary>
    public string MessageText => Localization.GetLocal("SyncRestart.Message");

    /// <summary>The text of the cancel-and-start-again button.</summary>
    public string RestartText => Localization.GetLocal("SyncRestart.Restart");

    /// <summary>The text of the keep button.</summary>
    public string KeepText => Localization.GetLocal("SyncRestart.Keep");

    /// <summary>Cancels the running sync and starts again.</summary>
    public IRelayCommand RestartCommand => field ??= new RelayCommand(() => Answer(SyncRestartChoiceFactory.CreateCancelAndRestart()));

    /// <summary>Keeps the running sync.</summary>
    public IRelayCommand KeepCommand => field ??= new RelayCommand(() => Answer(SyncRestartChoiceFactory.CreateKeepCurrent()));

    /// <inheritdoc />
    public Task<SyncRestartChoice> AskAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{dispatcher}{accountId}{cancellationToken}");

    private void Answer(SyncRestartChoice choice) => _ = pending.Match(answer => answer.TrySetResult(choice), () => false);
}
