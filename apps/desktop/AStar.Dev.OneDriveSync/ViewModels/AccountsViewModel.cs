using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountsViewModel : ReactiveObject
{
    private bool _isWizardVisible;
    private AddAccountWizardViewModel? _wizard;

    public AccountsViewModel()
    {
        AddAccountCommand = ReactiveCommand.Create(OpenAddAccountWizard);
    }

    public ObservableCollection<AccountCardViewModel> Accounts { get; } = [];

    public bool HasAccounts => Accounts.Count > 0;

    public bool IsWizardVisible
    {
        get => _isWizardVisible;
        set => this.RaiseAndSetIfChanged(ref _isWizardVisible, value);
    }

    public AddAccountWizardViewModel? Wizard
    {
        get => _wizard;
        set => this.RaiseAndSetIfChanged(ref _wizard, value);
    }

    public ICommand AddAccountCommand { get; }

    private void OpenAddAccountWizard()
    {
        IsWizardVisible = true;
        Wizard          = new AddAccountWizardViewModel();
    }
}
