using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDrive.Client.Features.FolderBrowsing;
using AStar.Dev.OneDriveSync.Infrastructure;
using ReactiveUI;
using AccessToken = AStar.Dev.OneDrive.Client.Features.Authentication.AccessToken;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     Three-step wizard for adding a new OneDrive account (AM-01, AM-02, AM-03).
///     Transient — a new instance is created for every wizard open.
/// </summary>
public sealed class AddAccountWizardViewModel : ViewModelBase
{
    private readonly IMsalClient _msalClient;
    private readonly IOneDriveFolderService _folderService;
    private readonly IAccountRepository _accountRepository;
    private readonly ILocalSyncPathService _pathService;

    private AccessToken? _acquiredToken;

    public AddAccountWizardViewModel(IMsalClient msalClient, IOneDriveFolderService folderService, IAccountRepository accountRepository, ILocalSyncPathService pathService)
    {
        _msalClient        = msalClient;
        _folderService     = folderService;
        _accountRepository = accountRepository;
        _pathService       = pathService;

        AuthenticateCommand = ReactiveCommand.CreateFromTask(AuthenticateAsync);
        LoadFoldersCommand  = ReactiveCommand.CreateFromTask(LoadFoldersAsync);
        AdvanceCommand      = ReactiveCommand.Create(Advance);
        BackCommand         = ReactiveCommand.Create(Back);
        CancelCommand       = ReactiveCommand.Create(Cancel);
        FinishCommand       = ReactiveCommand.CreateFromTask(FinishAsync);
    }

    /// <summary>Current wizard step (1 = Authenticate, 2 = Folder Selection, 3 = Confirm).</summary>
    public int CurrentStep
    {
        get;
        private set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(IsStep1));
            this.RaisePropertyChanged(nameof(IsStep2));
            this.RaisePropertyChanged(nameof(IsStep3));
            this.RaisePropertyChanged(nameof(IsNotStep1));
            this.RaisePropertyChanged(nameof(IsNotStep3));
        }
    } = 1;

    /// <summary>True when on the authentication step.</summary>
    public bool IsStep1 => CurrentStep == 1;

    /// <summary>True when on the folder selection step.</summary>
    public bool IsStep2 => CurrentStep == 2;

    /// <summary>True when on the confirmation step.</summary>
    public bool IsStep3 => CurrentStep == 3;

    /// <summary>True when past the authentication step (Back button enablement).</summary>
    public bool IsNotStep1 => CurrentStep != 1;

    /// <summary>True when not on the confirmation step (Next button visibility).</summary>
    public bool IsNotStep3 => CurrentStep != 3;

    /// <summary>Whether the user may navigate forward from the current step.</summary>
    public bool CanAdvance
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>User-visible error message from the last failed operation; null when no error.</summary>
    public string? ErrorMessage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>True once the wizard is cancelled by the user.</summary>
    public bool IsCancelled
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>True once the wizard completes successfully.</summary>
    public bool IsCompleted
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Whether folder loading is in progress (step 2).</summary>
    public bool IsLoadingFolders
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Display name returned from MSAL.</summary>
    public string AccountDisplayName
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Email returned from MSAL.</summary>
    public string AccountEmail
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Selected local sync path (editable in step 3).</summary>
    public string LocalSyncPath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Root-level OneDrive folders for selection in step 2.</summary>
    public ObservableCollection<OneDriveFolderViewModel> AvailableFolders { get; } = [];

    public ReactiveCommand<Unit, Unit> AuthenticateCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadFoldersCommand { get; }
    public ReactiveCommand<Unit, Unit> AdvanceCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> FinishCommand { get; }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        ErrorMessage = null;

        var result = await Task.Run(() => _msalClient.AcquireTokenInteractiveAsync(ct), ct).ConfigureAwait(false);

        result
            .Tap(tokenTuple =>
            {
                var (token, _) = tokenTuple;
                _acquiredToken     = token;
                AccountDisplayName = string.Empty;
                AccountEmail       = string.Empty;
                CurrentStep        = 2;
                CanAdvance         = false;
            })
            .TapError(error => ErrorMessage = error);
    }

    private async Task LoadFoldersAsync(CancellationToken ct)
    {
        if (_acquiredToken is null)
            return;

        IsLoadingFolders = true;
        ErrorMessage     = null;
        AvailableFolders.Clear();

        var result = await _folderService
            .GetRootFoldersAsync(_acquiredToken?.Token!, ct)
            .ConfigureAwait(false);

        result
            .Tap(folders =>
            {
                foreach (var folder in folders)
                    AvailableFolders.Add(new OneDriveFolderViewModel(folder, _folderService, _acquiredToken?.Token!));

                LocalSyncPath = _pathService.GetDefaultPath(AccountDisplayName.Length > 0 ? AccountDisplayName : "OneDrive");
                CanAdvance    = true;
            })
            .TapError(error =>
            {
                ErrorMessage = error;
                CanAdvance   = false;
            });

        IsLoadingFolders = false;
    }

    private void Advance()
    {
        if (CurrentStep < 3)
            CurrentStep++;
    }

    private void Back()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }

    private void Cancel() => IsCancelled = true;

    private async Task FinishAsync(CancellationToken ct)
    {
        var account = new Account
        {
            Id                 = Guid.NewGuid(),
            DisplayName        = AccountDisplayName,
            Email              = AccountEmail,
            MicrosoftAccountId = string.Empty,
            LocalSyncPath      = LocalSyncPath,
        };

        await _accountRepository.AddAsync(account, ct).ConfigureAwait(false);

        IsCompleted = true;
    }
}
