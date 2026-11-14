using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Conflicts;
using AStarDev.CloudSync.FolderSelection;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.SyncRestart;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;

namespace AStarDev.CloudSync.TestsUnit.Overlays;

internal sealed class OverlayCase
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private OverlayCase(Func<Control> createView, Func<MainWindowViewModel, object> dialog, Func<MainWindowViewModel, Task> open, Func<MainWindowViewModel, bool> isOpen, Action<MainWindowViewModel> close)
    {
        CreateView = createView;
        Dialog = dialog;
        Open = open;
        IsOpen = isOpen;
        Close = close;
    }

    public Func<Control> CreateView { get; }

    public Func<MainWindowViewModel, object> Dialog { get; }

    public Func<MainWindowViewModel, Task> Open { get; }

    public Func<MainWindowViewModel, bool> IsOpen { get; }

    public Action<MainWindowViewModel> Close { get; }

    public static OverlayCase For(string key) => key switch
    {
        "AddAccount" => new OverlayCase(() => new AddAccountView(), vm => vm.AddAccount, vm => { _ = vm.AddAccount.ShowAsync(TestContext.Current.CancellationToken); return Task.CompletedTask; }, vm => vm.AddAccount.IsOpen, vm => vm.AddAccount.LaterCommand.Execute(null)),
        "AccountDialog" => new OverlayCase(() => new AccountDialogView(), vm => vm.AccountDialog, vm => { _ = vm.AccountDialog.RemoveAsync(TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), vm.AccountDialog), TestContext.Current.CancellationToken); return Task.CompletedTask; }, vm => vm.AccountDialog.IsOpen, vm => vm.AccountDialog.CloseCommand.Execute(null)),
        "FolderSelection" => new OverlayCase(() => new FolderSelectionView(), vm => vm.FolderSelection, vm => { _ = vm.FolderSelection.ShowAsync(Option.Some(Account), TestContext.Current.CancellationToken); return Task.CompletedTask; }, vm => vm.FolderSelection.IsOpen, vm => vm.FolderSelection.CancelCommand.Execute(null)),
        "SyncRestart" => new OverlayCase(() => new ConfirmSyncRestartView(), vm => vm.SyncRestart, vm => { _ = vm.SyncRestart.AskAsync(Account, TestContext.Current.CancellationToken); return Task.CompletedTask; }, vm => vm.SyncRestart.IsOpen, vm => vm.SyncRestart.KeepCommand.Execute(null)),
        _ => new OverlayCase(() => new ConflictListView(), vm => vm.ConflictList, vm => vm.ConflictList.OpenCommand.ExecuteAsync(null), vm => vm.ConflictList.IsOpen, vm => vm.ConflictList.CloseCommand.Execute(null)),
    };
}
