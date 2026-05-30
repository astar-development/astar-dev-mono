using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Localization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed partial class AccountSyncSettingsViewModel : ObservableObject
{
    private readonly OneDriveAccount account;
    private readonly IAccountRepository repository;

    public AccountSyncSettingsViewModel(OneDriveAccount account, IAccountRepository repository, ILocalizationService loc)
    {
        this.account = account;
        this.repository = repository;
        LocalSyncPath = account.SyncConfig.Match(cfg => cfg.LocalSyncPath.Value, () => string.Empty);
        ConflictPolicy = account.SyncConfig.Match(cfg => cfg.ConflictPolicy, () => ConflictPolicy.Ignore);
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc);
        loc.CultureChanged += (_, _) => { PolicyOptions = ConflictPolicyOptionFactory.Create(loc); OnPropertyChanged(nameof(PolicyOptions)); };
    }

    /// <summary>Raw string account ID — unwrapped at the display boundary.</summary>
    public string AccountId => account.Id.Id;
    public string Email => account.Profile.Email;
    public string DisplayName => account.Profile.DisplayName;
    public string AccentHex => AccountCardViewModel.PaletteHex(account.AccentIndex);

    [ObservableProperty]
    public partial string LocalSyncPath { get; set; }

    [ObservableProperty]
    public partial ConflictPolicy ConflictPolicy { get; set; }

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; private set; }

    [RelayCommand]
    private static async Task BrowseAsync()
    {
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var resolvedPath = LocalSyncPathFactory.Create(LocalSyncPath).Match<Domain.LocalSyncPath?>(p => p, _ => null);
        account.SyncConfig = resolvedPath is null
            ? Option.None<AccountSyncConfig>()
            : Option.Some(AccountSyncConfigFactory.Create(ConflictPolicy, resolvedPath));

        await repository.GetByIdAsync(account.Id, CancellationToken.None)
            .TapAsync(async entity =>
            {
                entity.SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy, resolvedPath ?? Domain.LocalSyncPath.Restore(string.Empty));
                await repository.UpsertAsync(entity, CancellationToken.None);
            });
    }
}
