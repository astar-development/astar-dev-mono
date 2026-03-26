using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.Models;
using AStar.Dev.OneDriveSync.Services;
using Avalonia.Media;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

/// <summary>AM-01 → AM-08: Account list management, wizard orchestration, and persistence.</summary>
public class AccountsViewModel : ReactiveObject
{
    private static readonly Color[] AccentPalette = [Colors.DodgerBlue, Colors.MediumSlateBlue, Colors.Coral, Colors.MediumSeaGreen, Colors.Goldenrod, Colors.Orchid];

    private readonly IAccountStore _accountStore;
    private readonly IMsalAuthService _authService;
    private readonly IOneDriveFolderService _folderService;
    private readonly List<AccountRecord> _records = [];

    public AccountsViewModel(IAccountStore accountStore, IMsalAuthService authService, IOneDriveFolderService folderService)
    {
        _accountStore = accountStore;
        _authService = authService;
        _folderService = folderService;

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

    /// <summary>AM-08: Shown when user clicks remove and local folder exists.</summary>
    public bool IsRemovePromptVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string RemovePromptAccountEmail
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public ICommand AddAccountCommand { get; }

    public ICommand? ConfirmRemoveKeepCommand
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand? ConfirmRemoveDeleteCommand
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand? CancelRemoveCommand
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>AM-06: Validates that the proposed path does not overlap with any existing account's path.</summary>
    public string? ValidateLocalSyncPath(string proposedPath, string? excludeAccountId = null)
    {
        var normalised = NormalisePath(proposedPath);
        foreach (var record in _records)
        {
            if (record.AccountId == excludeAccountId)
            {
                continue;
            }

            var existing = NormalisePath(record.LocalSyncPath);
            if (normalised.StartsWith(existing, StringComparison.OrdinalIgnoreCase) || existing.StartsWith(normalised, StringComparison.OrdinalIgnoreCase))
            {
                return $"Path overlaps with account '{record.Email}'. Each account must use a unique, non-overlapping folder.";
            }
        }

        return null;
    }

    public async Task LoadAccountsAsync(CancellationToken ct = default)
    {
        var records = await _accountStore.LoadAsync(ct);
        _records.Clear();
        _records.AddRange(records);

        Accounts.Clear();
        foreach (var r in records)
        {
            Accounts.Add(CreateCard(r));
        }

        this.RaisePropertyChanged(nameof(HasAccounts));
    }

    private void OpenAddAccountWizard()
    {
        IsWizardVisible = true;
        Wizard = new AddAccountWizardViewModel(_authService, _folderService, CloseWizard, OnWizardFinished);
    }

    private void CloseWizard()
    {
        IsWizardVisible = false;
        Wizard = null;
    }

    private async void OnWizardFinished(MsalAuthResult auth, IReadOnlyList<WizardFolderItem> selectedFolders, string localSyncPath)
    {
        var overlap = ValidateLocalSyncPath(localSyncPath);
        if (overlap is not null)
        {
            if (Wizard is not null)
            {
                Wizard.ErrorText = overlap;
            }

            return;
        }

        var record = new AccountRecord
        {
            AccountId = auth.AccountId,
            Email = auth.Email,
            DisplayName = auth.DisplayName,
            LocalSyncPath = localSyncPath,
            SelectedFolders = selectedFolders.Where(f => f.IsSelected).Select(f => new SelectedFolder { FolderId = f.FolderId, Name = f.Name }).ToList()
        };

        _records.Add(record);
        Accounts.Add(CreateCard(record));
        this.RaisePropertyChanged(nameof(HasAccounts));

        await PersistAsync();
        CloseWizard();
    }

    private AccountCardViewModel CreateCard(AccountRecord record)
    {
        var accentColor = AccentPalette[Accounts.Count % AccentPalette.Length];
        var initials = GetInitials(record.DisplayName, record.Email);

        return new AccountCardViewModel
        {
            AccountId = record.AccountId,
            Email = record.Email,
            DisplayName = string.IsNullOrEmpty(record.DisplayName) ? record.Email : record.DisplayName,
            Initials = initials,
            AccentColor = accentColor,
            LastSyncText = "Never synced",
            RemoveCommand = ReactiveCommand.Create(() => PromptRemoveAccount(record.AccountId))
        };
    }

    /// <summary>AM-08: Show keep/delete prompt before removing.</summary>
    private void PromptRemoveAccount(string accountId)
    {
        var record = _records.FirstOrDefault(r => r.AccountId == accountId);
        if (record is null)
        {
            return;
        }

        RemovePromptAccountEmail = record.Email;

        ConfirmRemoveKeepCommand = ReactiveCommand.Create(async () => await RemoveAccountAsync(accountId, deleteLocalFolder: false));
        ConfirmRemoveDeleteCommand = ReactiveCommand.Create(async () => await RemoveAccountAsync(accountId, deleteLocalFolder: true));
        CancelRemoveCommand = ReactiveCommand.Create(() => IsRemovePromptVisible = false);

        IsRemovePromptVisible = true;
    }

    private async Task RemoveAccountAsync(string accountId, bool deleteLocalFolder)
    {
        IsRemovePromptVisible = false;

        var record = _records.FirstOrDefault(r => r.AccountId == accountId);
        if (record is null)
        {
            return;
        }

        if (deleteLocalFolder && Directory.Exists(record.LocalSyncPath))
        {
            try
            {
                Directory.Delete(record.LocalSyncPath, recursive: true);
            }
            catch
            {
                // Best-effort deletion; user can manually clean up.
            }
        }

        try
        {
            await _authService.SignOutAsync(accountId);
        }
        catch
        {
            // Best-effort sign out.
        }

        _records.Remove(record);
        var card = Accounts.FirstOrDefault(c => c.AccountId == accountId);
        if (card is not null)
        {
            Accounts.Remove(card);
        }

        this.RaisePropertyChanged(nameof(HasAccounts));
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        try
        {
            await _accountStore.SaveAsync(_records);
        }
        catch
        {
            // Persistence failure is non-fatal for the UI.
        }
    }

    private static string NormalisePath(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

    private static string GetInitials(string displayName, string email)
    {
        var source = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
        var parts = source.Split(' ', '@', '.').Where(p => p.Length > 0).ToArray();
        return parts.Length >= 2 ? $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}" : parts.Length == 1 ? $"{char.ToUpperInvariant(parts[0][0])}" : "?";
    }
}
