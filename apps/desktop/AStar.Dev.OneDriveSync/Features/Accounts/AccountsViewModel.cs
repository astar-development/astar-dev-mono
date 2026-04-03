using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     View model for the Accounts view (Section 7, AM-01 to AM-15).
/// </summary>
public sealed class AccountsViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthStateService _authStateService;
    private readonly IRelativeTimeFormatter _timeFormatter;
    private readonly Func<AddAccountWizardViewModel> _wizardFactory;
    private readonly IDisposable _authStateSubscription;

    public AccountsViewModel(IAccountRepository accountRepository, IAuthStateService authStateService, IRelativeTimeFormatter timeFormatter, Func<AddAccountWizardViewModel> wizardFactory)
    {
        _accountRepository = accountRepository;
        _authStateService  = authStateService;
        _timeFormatter     = timeFormatter;
        _wizardFactory     = wizardFactory;

        ShowWizardCommand = ReactiveCommand.Create(OpenWizard);

        _authStateSubscription = _authStateService.AccountAuthStateChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(HandleAuthStateChange);
    }

    /// <summary>All configured accounts.</summary>
    public ObservableCollection<AccountRowViewModel> Accounts { get; } = [];

    /// <summary>The active wizard instance; null when wizard is not open.</summary>
    public AddAccountWizardViewModel? ActiveWizard
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Whether the Add Account wizard is visible.</summary>
    public bool IsWizardOpen => ActiveWizard is not null;

    /// <summary>Error message from the most recent failed operation; null when no error.</summary>
    public string? ErrorMessage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> ShowWizardCommand { get; }

    /// <summary>Loads accounts from the database. Must be called after construction.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetAllAsync(ct).ConfigureAwait(false);

        var rows = accounts.Select(account => new AccountRowViewModel(
            account,
            _timeFormatter,
            onRemove:  row => _ = RemoveAccountAsync(row.AccountId, ct),
            onSyncNow: _   => { }));

        RxApp.MainThreadScheduler.Schedule(() =>
        {
            Accounts.Clear();
            foreach (var row in rows)
                Accounts.Add(row);
        });
    }

    private void OpenWizard()
    {
        var wizard = _wizardFactory();

        wizard.WhenAnyValue(viewModel => viewModel.IsCompleted)
            .Where(completed => completed)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => OnWizardCompleted());

        wizard.WhenAnyValue(viewModel => viewModel.IsCancelled)
            .Where(cancelled => cancelled)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => CloseWizard());

        ActiveWizard = wizard;
        this.RaisePropertyChanged(nameof(IsWizardOpen));
    }

    private void OnWizardCompleted()
    {
        CloseWizard();
        _ = LoadAsync();
    }

    private void CloseWizard()
    {
        ActiveWizard = null;
        this.RaisePropertyChanged(nameof(IsWizardOpen));
    }

    private async Task RemoveAccountAsync(Guid accountId, CancellationToken ct)
    {
        await _accountRepository.RemoveAsync(accountId, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
    }

    private void HandleAuthStateChange((Guid AccountId, AccountAuthState NewState) change)
    {
        var row = Accounts.FirstOrDefault(account => account.AccountId == change.AccountId);
        row?.UpdateAuthState(change.NewState);
    }

    public void Dispose() => _authStateSubscription.Dispose();
}
