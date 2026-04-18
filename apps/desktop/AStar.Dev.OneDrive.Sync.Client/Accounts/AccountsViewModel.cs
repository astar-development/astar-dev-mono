using System.Collections.ObjectModel;
using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed partial class AccountsViewModel(IAuthService authService, IGraphService graphService, IAccountRepository repository, ISyncEventAggregator syncEventAggregator) : ObservableObject
{
    public ObservableCollection<AccountCardViewModel> Accounts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    public partial AccountCardViewModel? ActiveAccount { get; set; }

    public bool HasAccounts => Accounts.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWizardVisible))]
    public partial Onboarding.AddAccountWizardViewModel? Wizard { get; set; }

    public bool IsWizardVisible => Wizard is not null;

    /// <summary>Raised when user clicks an account card — navigate to Files.</summary>
    public event EventHandler<AccountCardViewModel>? AccountSelected;

    /// <summary>Raised after a new account is successfully added and persisted.</summary>
    public event EventHandler<OneDriveAccount>? AccountAdded;

    /// <summary>Raised after an account is removed.</summary>
    public event EventHandler<string>? AccountRemoved;

    /// <summary>Raised when a sync event changes the active account's card state.</summary>
    public event EventHandler? ActiveAccountStateChanged;

    public void SubscribeToSyncEvents()
    {
        syncEventAggregator.SyncProgressChanged += OnSyncProgressChanged;
        syncEventAggregator.SyncCompleted += OnSyncCompleted;
        syncEventAggregator.ConflictDetected += OnConflictDetected;
    }

    public void AddAccount()
    {
        var wizard = new Onboarding.AddAccountWizardViewModel(authService, graphService);
        wizard.Completed += OnWizardCompletedAsync;
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
        await repository.DeleteAsync(new AccountId(card.Id), CancellationToken.None);

        _ = Accounts.Remove(card);

        if(ActiveAccount == card)
            ActiveAccount = Accounts.FirstOrDefault();

        OnPropertyChanged(nameof(HasAccounts));
        AccountRemoved?.Invoke(this, card.Id);
    }

    private async void OnWizardCompletedAsync(object? sender, OneDriveAccount account)
        => await Try.RunAsync(async () =>
            {
                CloseWizard();

                account.AccentIndex = Accounts.Count % 6;
                account.IsActive = Accounts.Count == 0;
                if(account.LocalSyncPath is null)
                {
                    var defaultPath = ApplicationMetadata.ApplicationNameLowered.UserDirectory().CombinePath(account.Email);
                    account.LocalSyncPath = LocalSyncPathFactory.Create(defaultPath).Match<LocalSyncPath?>(p => p, _ => null);
                }

                var entity = ToEntity(account);
                await repository.UpsertAsync(entity, CancellationToken.None);

                if(account.IsActive)
                    await repository.SetActiveAccountAsync(account.Id, CancellationToken.None);

                var card = BuildCard(account);
                Accounts.Add(card);
                OnPropertyChanged(nameof(HasAccounts));

                if(account.IsActive)
                    ActiveAccount = card;

                AccountAdded?.Invoke(this, account);
                return Unit.Default;
            })
            .TapErrorAsync(error => Serilog.Log.Error(error, "Error completing account onboarding wizard"));

    private void OnWizardCancelled(object? sender, EventArgs e) => CloseWizard();

    private void CloseWizard()
    {
        if(Wizard is not null)
        {
            Wizard.Completed -= OnWizardCompletedAsync;
            Wizard.Cancelled -= OnWizardCancelled;
        }

        Wizard = null;
    }

    private void OnCardSelected(object? sender, AccountCardViewModel card)
    {
        foreach(var accountCard in Accounts)
            accountCard.IsActive = accountCard == card;

        ActiveAccount = card;
        AccountSelected?.Invoke(this, card);

        _ = repository.SetActiveAccountAsync(new AccountId(card.Id), CancellationToken.None);
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs args)
    {
        var card = Accounts.FirstOrDefault(a => a.Id == args.AccountId);
        if(card is null)
            return;

        card.SyncState = args.SyncState;

        if(card.Id == ActiveAccount?.Id)
            ActiveAccountStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSyncCompleted(object? sender, string accountId)
    {
        var card = Accounts.FirstOrDefault(a => a.Id == accountId);
        if(card is null)
            return;

        card.SyncState = SyncState.Completed;

        if(card.Id == ActiveAccount?.Id)
            ActiveAccountStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnConflictDetected(object? sender, SyncConflict conflict)
    {
        var card = Accounts.FirstOrDefault(a => a.Id == conflict.AccountId);
        if(card is null)
            return;

        card.ConflictCount++;

        if(card.Id == ActiveAccount?.Id)
            ActiveAccountStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private AccountCardViewModel BuildCard(OneDriveAccount account)
    {
        var card = new AccountCardViewModel(account);
        card.Selected += OnCardSelected;
        card.RemoveRequested += (_, accountCard) => RemoveAccountCommand.Execute(accountCard);
        return card;
    }

    private static AccountEntity ToEntity(OneDriveAccount a)
        => new()
        {
            Id            = a.Id,
            DisplayName   = a.DisplayName,
            Email         = a.Email,
            AccentIndex   = a.AccentIndex,
            IsActive      = a.IsActive,
            DeltaLink     = a.DeltaLink,
            LastSyncedAt  = a.LastSyncedAt,
            QuotaTotal    = a.QuotaTotal,
            LocalSyncPath = a.LocalSyncPath ?? LocalSyncPath.Restore(string.Empty),
            ConflictPolicy = a.ConflictPolicy,
            QuotaUsed     = a.QuotaUsed,
            SyncFolders   = [.. a.FolderNames.Select(kvp => new SyncFolderEntity
                {
                    FolderId   = kvp.Key,
                    FolderName = kvp.Value,
                    AccountId  = a.Id,
                    IsIncluded = a.SelectedFolderIds.Contains(kvp.Key)
                })]
        };
}
