using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountsViewModel : ReactiveObject
{
    public AccountsViewModel()
    {
        AddAccountCommand = ReactiveCommand.Create(OpenAddAccountWizard);
    }

    public ObservableCollection<AccountCardViewModel> Accounts { get; } = [];

    public bool HasAccounts => Accounts.Count > 0;

    public bool IsWizardVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public AddAccountWizardViewModel? Wizard
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand AddAccountCommand { get; }

    private void OpenAddAccountWizard()
    {
        IsWizardVisible = true;
        Wizard          = new AddAccountWizardViewModel();
    }
}
