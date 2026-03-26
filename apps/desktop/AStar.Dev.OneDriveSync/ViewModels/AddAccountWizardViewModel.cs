using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AddAccountWizardViewModel : ReactiveObject
{
    private int _step = 1;

    public bool IsSignInStep       => _step == 1;
    public bool IsSelectFoldersStep => _step == 2;
    public bool IsConfirmStep      => _step == 3;
    public bool CanGoBack          => _step > 1;
    public bool CanGoNext          => _step < 3 || IsSignedIn;
    public string NextLabel        => _step == 3 ? "Finish" : "Next";

    public string ConfirmedEmail { get; set; } = string.Empty;
    public int ConfirmedFolderCount { get; set; }
    public string LocalSyncPath { get; set; } = string.Empty;

    public bool IsWaitingForAuth
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsSignedIn
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string SignInStatusText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public ObservableCollection<WizardFolderItem> Folders { get; } = [];

    public ICommand OpenBrowserCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand SkipFoldersCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand BackCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand CancelCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand NextCommand { get; init; } = ReactiveCommand.Create(() => { });
}
