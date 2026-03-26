using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.old.Services;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

/// <summary>AM-02: 3-step add-account wizard (authenticate → select folders → confirm).</summary>
public class AddAccountWizardViewModel : ReactiveObject
{
    private readonly IMsalAuthService _authService;
    private readonly IOneDriveFolderService _folderService;
    private string _accessToken = string.Empty;

    public AddAccountWizardViewModel(IMsalAuthService authService, IOneDriveFolderService folderService, Action onCancel, Action<MsalAuthResult, IReadOnlyList<WizardFolderItem>, string> onFinish)
    {
        _authService = authService;
        _folderService = folderService;

        OpenBrowserCommand = ReactiveCommand.CreateFromTask(SignInAsync);
        SkipFoldersCommand = ReactiveCommand.Create(SkipFolders);

        BackCommand = ReactiveCommand.Create(() =>
        {
            Step--;
            RaiseStepChanged();
        });

        CancelCommand = ReactiveCommand.Create(onCancel);

        NextCommand = ReactiveCommand.Create(() =>
        {
            if (Step == 3)
            {
                var selectedFolders = Folders.Where(f => f.IsSelected).ToList();
                onFinish(new MsalAuthResult(AccountId, ConfirmedEmail, ConfirmedEmail, _accessToken), selectedFolders, LocalSyncPath);
                return;
            }

            if (Step == 2)
            {
                ConfirmedFolderCount = Folders.Count(f => f.IsSelected);
            }

            Step++;
            RaiseStepChanged();
        });
    }

    private int Step
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = 1;

    public bool IsSignInStep => Step == 1;
    public bool IsSelectFoldersStep => Step == 2;
    public bool IsConfirmStep => Step == 3;
    public bool CanGoBack => Step > 1;
    public bool CanGoNext => Step switch { 1 => IsSignedIn, 2 => true, 3 => true, _ => false };
    public string NextLabel => Step == 3 ? "Finish" : "Next";

    public string AccountId { get; private set; } = string.Empty;

    public string ConfirmedEmail
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public int ConfirmedFolderCount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string LocalSyncPath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

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

    public string ErrorText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public ObservableCollection<WizardFolderItem> Folders { get; } = [];

    public ICommand OpenBrowserCommand { get; }
    public ICommand SkipFoldersCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand NextCommand { get; }

    private async Task SignInAsync(CancellationToken ct)
    {
        IsWaitingForAuth = true;
        SignInStatusText = "Waiting for browser sign-in\u2026";
        ErrorText = string.Empty;

        var signInResult = await _authService.SignInInteractiveAsync(ct);

        signInResult.Match(
            onSuccess: authResult =>
            {
                _accessToken = authResult.AccessToken;
                AccountId = authResult.AccountId;
                ConfirmedEmail = authResult.Email;
                LocalSyncPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", SanitiseAccountName(authResult.Email));
                IsSignedIn = true;
                SignInStatusText = $"Signed in as {authResult.Email}";
                return true;
            },
            onFailure: error =>
            {
                SignInStatusText = "Sign-in failed.";
                ErrorText = error;
                return false;
            });

        if (IsSignedIn)
        {
            await LoadRootFoldersAsync(ct);
        }

        IsWaitingForAuth = false;
        RaiseStepChanged();
    }

    private async Task LoadRootFoldersAsync(CancellationToken ct)
    {
        var foldersResult = await _folderService.GetRootFoldersAsync(_accessToken, ct);

        foldersResult.Match(
            onSuccess: folders =>
            {
                Folders.Clear();
                foreach (var f in folders)
                {
                    Folders.Add(new WizardFolderItem { FolderId = f.Id, Name = f.Name, IsSelected = true });
                }

                return true;
            },
            onFailure: _ => false);
    }

    private void SkipFolders()
    {
        foreach (var f in Folders)
        {
            f.IsSelected = true;
        }

        ConfirmedFolderCount = Folders.Count;
        Step = 3;
        RaiseStepChanged();
    }

    private void RaiseStepChanged()
    {
        this.RaisePropertyChanged(nameof(IsSignInStep));
        this.RaisePropertyChanged(nameof(IsSelectFoldersStep));
        this.RaisePropertyChanged(nameof(IsConfirmStep));
        this.RaisePropertyChanged(nameof(CanGoBack));
        this.RaisePropertyChanged(nameof(CanGoNext));
        this.RaisePropertyChanged(nameof(NextLabel));
    }

    private static string SanitiseAccountName(string email)
    {
        var name = email.Contains('@') ? email[..email.IndexOf('@')] : email;
        return string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
    }
}
