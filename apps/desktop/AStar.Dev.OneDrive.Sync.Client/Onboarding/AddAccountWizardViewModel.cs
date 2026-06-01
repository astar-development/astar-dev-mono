using System.Collections.ObjectModel;
using System.Globalization;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace AStar.Dev.OneDrive.Sync.Client.Onboarding;

public sealed partial class AddAccountWizardViewModel : ObservableObject, IDisposable
{
    private readonly IAuthService authService;
    private readonly IGraphService graphService;
    private readonly ILocalizationService loc;
    private string _accountId = string.Empty;
    private string? _accessToken;
    private CancellationTokenSource? _authCts;

    public AddAccountWizardViewModel(IAuthService authService, IGraphService graphService, ILocalizationService localizationService)
    {
        this.authService = authService;
        this.graphService = graphService;
        loc = localizationService;
        loc.CultureChanged += OnCultureChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSignInStep))]
    [NotifyPropertyChangedFor(nameof(IsSelectFoldersStep))]
    [NotifyPropertyChangedFor(nameof(IsConfirmStep))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    public partial WizardStep CurrentStep { get; set; } = WizardStep.SignIn;

    public bool IsSignInStep => CurrentStep == WizardStep.SignIn;
    public bool IsSelectFoldersStep => CurrentStep == WizardStep.SelectFolders;
    public bool IsConfirmStep => CurrentStep == WizardStep.Confirm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    public partial bool IsSignedIn { get; set; }

    [ObservableProperty]
    public partial bool IsWaitingForAuth { get; set; }

    [ObservableProperty]
    public partial string SignInStatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool SignInHasError { get; set; }
    public ObservableCollection<WizardFolderItem> Folders { get; } = [];

    [ObservableProperty]
    public partial bool IsLoadingFolders { get; set; }

    [ObservableProperty]
    public partial string FolderLoadError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmedDisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmedEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ConfirmedFolderCount { get; set; }

    public bool CanGoBack => CurrentStep != WizardStep.SignIn;
    public bool CanGoNext => CurrentStep switch
    {
        WizardStep.SignIn => IsSignedIn,
        WizardStep.SelectFolders => true,
        WizardStep.Confirm => true,
        _ => false
    };

    /// <summary>The label for the primary action button, localised for the current culture.</summary>
    public string NextLabel => CurrentStep == WizardStep.Confirm ? loc.GetLocal("Wizard.AddAccount.Finish") : loc.GetLocal("Wizard.AddAccount.Next");

    [RelayCommand]
    private void Back()
    {
        if(CurrentStep == WizardStep.SelectFolders)
            CurrentStep = WizardStep.SignIn;
        else if(CurrentStep == WizardStep.Confirm)
            CurrentStep = WizardStep.SelectFolders;
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextAsync()
    {
        switch(CurrentStep)
        {
            case WizardStep.SignIn:
                CurrentStep = WizardStep.SelectFolders;
                await LoadFoldersAsync();
                break;

            case WizardStep.SelectFolders:
                BuildConfirmSummary();
                CurrentStep = WizardStep.Confirm;
                break;

            case WizardStep.Confirm:
                Finish();
                break;
        }
    }

    [RelayCommand]
    private void SkipFolders()
    {
        foreach(var f in Folders)
            f.IsSelected = false;
        BuildConfirmSummary();
        CurrentStep = WizardStep.Confirm;
    }

    [RelayCommand]
    private async Task OpenBrowserAsync()
    {
        if(IsWaitingForAuth)
            return;

        SetInitialSignInState();

        _authCts = new CancellationTokenSource();

        try
        {
            var result = await authService.SignInInteractiveAsync(_authCts.Token);
            _ = result.Match<bool>(
                ok    => { UpdateSuccessfulLoginState(ok); return true; },
                error => { DispatchAuthError(error); return false; });
        }
        finally
        {
            SetFinalSignInState();
        }
    }

    private void DispatchAuthError(AuthError error)
    {
        switch(error)
        {
            case AuthCancelledError: SetCancelledLoginState(); break;
            case AuthFailedError failed: SetFailedLoginState(failed); break;
            default: SetFailedLoginState(new AuthFailedError("Unexpected authentication error.")); break;
        }
    }

    private void SetFailedLoginState(AuthFailedError failed)
    {
        SignInStatusText = failed.Message;
        SignInHasError = true;
    }

    private void SetCancelledLoginState()
    {
        SignInStatusText = "Sign-in cancelled.";
        SignInHasError = false;
    }

    private void UpdateSuccessfulLoginState(AuthResult authResult)
    {
        _accountId = authResult.AccountId;
        _accessToken = authResult.AccessToken;
        ConfirmedDisplayName = authResult.Profile.DisplayName;
        ConfirmedEmail = authResult.Profile.Email;
        IsSignedIn = true;
        SignInStatusText = $"Signed in as {ConfirmedEmail}";
        SignInHasError = false;
        NextCommand.NotifyCanExecuteChanged();
    }

    private void SetFinalSignInState()
    {
        IsWaitingForAuth = false;
        _authCts?.Dispose();
        _authCts = null;
    }

    private void SetInitialSignInState()
    {
        SignInHasError = false;
        SignInStatusText = "Waiting for sign-in ...";
        IsWaitingForAuth = true;
    }

    public event EventHandler<OneDriveAccount>? Completed;
    public event EventHandler? Cancelled;

    [RelayCommand]
    private async Task CancelAsync()
    {
        if(_authCts is not null)
            await _authCts.CancelAsync();

        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadFoldersAsync()
    {
        if(_accessToken is null)
            return;

        IsLoadingFolders = true;
        FolderLoadError = string.Empty;
        Folders.Clear();

        try
        {
            var folders = await graphService.GetRootFoldersAsync(_accountId, _ => Task.FromResult(_accessToken ?? string.Empty))
                .MatchAsync<List<DriveFolder>, string, List<DriveFolder>?>(
                    f => f,
                    error =>
                    {
                        FolderLoadError = $"Could not load folders: {error}";
                        return null;
                    });

            if(folders is null)
                return;

            foreach(var f in folders)
            {
                Folders.Add(new WizardFolderItem(f.Id, f.Name)
                {
                    IsSelected = f.Name is "Documents" or "Desktop"
                });
            }
        }
        catch(Exception ex)
        {
            FolderLoadError = $"Could not load folders: {ex.Message}";
        }
        finally
        {
            IsLoadingFolders = false;
        }
    }

    private void BuildConfirmSummary()
        => ConfirmedFolderCount = Folders.Count(f => f.IsSelected);

    private void Finish()
    {
        var account = OneDriveAccountFactory.CreateFromWizardResult(
            _accountId, AccountProfileFactory.Create(ConfirmedDisplayName, ConfirmedEmail),
            Folders.Where(f => f.IsSelected));
        Completed?.Invoke(this, account);
    }

    private void OnCultureChanged(object? sender, CultureInfo culture) => OnPropertyChanged(nameof(NextLabel));

    public void Dispose()
    {
        loc.CultureChanged -= OnCultureChanged;
        _authCts?.Dispose();
    }
}
