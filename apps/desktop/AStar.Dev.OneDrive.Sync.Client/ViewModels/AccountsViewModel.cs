using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class AccountsViewModel(IAuthService authService, IGraphService graphService, IAccountRepository repository) : ObservableObject
{
    public ObservableCollection<AccountCardViewModel> Accounts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    private AccountCardViewModel? _activeAccount;

    public bool HasAccounts => Accounts.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWizardVisible))]
    private AddAccountWizardViewModel? _wizard;

    public bool IsWizardVisible => Wizard is not null;

    /// <summary>Raised when user clicks an account card — navigate to Files.</summary>
    public event EventHandler<AccountCardViewModel>? AccountSelected;

    /// <summary>Raised after a new account is successfully added and persisted.</summary>
    public event EventHandler<OneDriveAccount>? AccountAdded;

    /// <summary>Raised after an account is removed.</summary>
    public event EventHandler<string>? AccountRemoved;

    public void AddAccount()
    {
        var wizard = new AddAccountWizardViewModel(authService, graphService);
        wizard.Completed += OnWizardCompleted;
        wizard.Cancelled += OnWizardCancelled;
        Wizard = wizard;
    }

    public void RestoreAccounts(IEnumerable<OneDriveAccount> accounts)
    {
        foreach(var account in accounts)
        {
            var card = BuildCard(account);
            Accounts.Add(card);

            if(account.IsActive)
                ActiveAccount = card;
        }

        OnPropertyChanged(nameof(HasAccounts));
    }

    [RelayCommand]
    private async Task RemoveAccountAsync(AccountCardViewModel card)
    {
        await authService.SignOutAsync(card.Id);
        await repository.DeleteAsync(card.Id);

        _ = Accounts.Remove(card);

        if(ActiveAccount == card)
            ActiveAccount = Accounts.FirstOrDefault();

        OnPropertyChanged(nameof(HasAccounts));
        AccountRemoved?.Invoke(this, card.Id);
    }

    private async void OnWizardCompleted(object? sender, OneDriveAccount account)
    {
        CloseWizard();

        account.AccentIndex = Accounts.Count % 6;
        account.IsActive = Accounts.Count == 0;
        if(account.LocalSyncPath.IsNullOrWhiteSpace())
            account.LocalSyncPath = App.GetPlatformUserDataDirectory(account.Email);

        var entity = ToEntity(account);
        await repository.UpsertAsync(entity);

        if(account.IsActive)
            await repository.SetActiveAccountAsync(account.Id);

        var card = BuildCard(account);
        Accounts.Add(card);
        OnPropertyChanged(nameof(HasAccounts));

        if(account.IsActive)
            ActiveAccount = card;

        AccountAdded?.Invoke(this, account);
        System.Diagnostics.Debug.WriteLine($"AccountsViewModel instance: {GetHashCode()} Count: {Accounts.Count} - OnWizardCompleted: Added account {account.Email} with ID {account.Id}");
    }

    private void OnWizardCancelled(object? sender, EventArgs e) => CloseWizard();

    private void CloseWizard()
    {
        if(Wizard is not null)
        {
            Wizard.Completed -= OnWizardCompleted;
            Wizard.Cancelled -= OnWizardCancelled;
        }

        Wizard = null;
    }

    private void OnCardSelected(object? sender, AccountCardViewModel card)
    {
        foreach(var c in Accounts)
            c.IsActive = c == card;

        ActiveAccount = card;
        AccountSelected?.Invoke(this, card);

        _ = repository.SetActiveAccountAsync(card.Id);
    }

    private AccountCardViewModel BuildCard(OneDriveAccount account)
    {
        var card = new AccountCardViewModel(account);
        card.Selected += OnCardSelected;
        card.RemoveRequested += (_, c) => RemoveAccountCommand.Execute(c);
        return card;
    }

    private static AccountEntity ToEntity(OneDriveAccount a)
        => new()
        {
            Id = a.Id,
            DisplayName = a.DisplayName,
            Email = a.Email,
            AccentIndex = a.AccentIndex,
            IsActive = a.IsActive,
            DeltaLink = a.DeltaLink,
            LastSyncedAt = a.LastSyncedAt,
            QuotaTotal = a.QuotaTotal,
            LocalSyncPath = a.LocalSyncPath,
            ConflictPolicy = a.ConflictPolicy,
            QuotaUsed = a.QuotaUsed,
            SyncFolders = [.. a.SelectedFolderIds.Select(id => new SyncFolderEntity
                {
                    FolderId   = id,
                    FolderName = a.FolderNames.GetValueOrDefault(id, string.Empty),
                    AccountId  = a.Id
                })]
        };
}
