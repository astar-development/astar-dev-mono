using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Onboarding;

public enum WizardStep { SignIn, SelectFolders, Confirm }

public sealed partial class WizardFolderItem(string id, string name) : ObservableObject
{
    public string Id { get; } = id;
    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = true;
}

public sealed partial class AddAccountWizardViewModel(IAuthService authService, IGraphService graphService) : ObservableObject, IDisposable
{
    private string  _accountId   = string.Empty;
    private string? _accessToken;
    private CancellationTokenSource? _authCts;

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

    public string NextLabel => CurrentStep == WizardStep.Confirm ? "Finish" : "Next";

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

        SignInHasError = false;
        SignInStatusText = "Waiting for sign-in ...";
        IsWaitingForAuth = true;

        _authCts = new CancellationTokenSource();

        try
        {
            var result = await authService.SignInInteractiveAsync(_authCts.Token);

            if(result.IsCancelled)
            {
                SignInStatusText = "Sign-in cancelled.";
                SignInHasError = false;
            }
            else if(result.IsError)
            {
                SignInStatusText = result.ErrorMessage ?? "Sign-in failed.";
                SignInHasError = true;
            }
            else
            {
                _accountId = result.AccountId!;
                _accessToken = result.AccessToken;
                ConfirmedDisplayName = result.DisplayName ?? string.Empty;
                ConfirmedEmail = result.Email ?? string.Empty;
                IsSignedIn = true;
                SignInStatusText = $"Signed in as {ConfirmedEmail}";
                SignInHasError = false;
                NextCommand.NotifyCanExecuteChanged();
            }
        }
        finally
        {
            IsWaitingForAuth = false;
            _authCts.Dispose();
            _authCts = null;
        }
    }

    public event EventHandler<OneDriveAccount>? Completed;
    public event EventHandler?                  Cancelled;

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
            var driveFolders = await graphService
                .GetRootFoldersAsync(_accessToken);

            foreach(var f in driveFolders)
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
        var account = new OneDriveAccount
        {
            Id                = new AccountId(_accountId),
            DisplayName       = ConfirmedDisplayName,
            Email             = ConfirmedEmail,
            SelectedFolderIds = [.. Folders.Where(f => f.IsSelected).Select(f => new OneDriveFolderId(f.Id))],
            FolderNames       = Folders
                .Where(f => f.IsSelected)
                .ToDictionary(f => new OneDriveFolderId(f.Id), f => f.Name)
        };
        Completed?.Invoke(this, account);
    }

    public void Dispose() => _authCts?.Dispose();
}
