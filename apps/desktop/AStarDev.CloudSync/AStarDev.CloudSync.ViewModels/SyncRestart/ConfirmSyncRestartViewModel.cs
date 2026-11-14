using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.SyncRestart;

/// <summary>The SE-08 question shown when Sync Now is pressed while the account is syncing: cancel the current sync and start again, or keep it (#1402).</summary>
public sealed class ConfirmSyncRestartViewModel(ILocalizationService localization) : LocalizedObservableObject(localization), IConfirmSyncRestartDialog
{
    /// <summary>Whether the question is showing.</summary>
    public bool IsOpen => throw new NotImplementedException($"{Localization}");

    /// <summary>The dialog heading.</summary>
    public string TitleText => throw new NotImplementedException($"{Localization}");

    /// <summary>The question.</summary>
    public string MessageText => throw new NotImplementedException($"{Localization}");

    /// <summary>The label of the button that cancels the current sync and starts again.</summary>
    public string CancelAndRestartText => throw new NotImplementedException($"{Localization}");

    /// <summary>The label of the button that keeps the current sync.</summary>
    public string KeepText => throw new NotImplementedException($"{Localization}");

    /// <summary>Answers "cancel the current sync and start again".</summary>
    public IRelayCommand CancelAndRestartCommand => throw new NotImplementedException($"{Localization}");

    /// <summary>Answers "keep the current sync".</summary>
    public IRelayCommand KeepCommand => throw new NotImplementedException($"{Localization}");

    /// <inheritdoc />
    public Task<SyncRestartChoice> AskAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{cancellationToken}");
}
