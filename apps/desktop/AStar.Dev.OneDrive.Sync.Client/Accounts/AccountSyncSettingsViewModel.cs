using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed partial class AccountSyncSettingsViewModel(OneDriveAccount account, IAccountRepository repository) : ObservableObject
{
    /// <summary>Raw string account ID — unwrapped at the display boundary.</summary>
    public string AccountId => account.Id.Id;
    public string Email => account.Profile.Email;
    public string DisplayName => account.Profile.DisplayName;
    public string AccentHex => AccountCardViewModel.PaletteHex(account.AccentIndex);

    [ObservableProperty]
    public partial string LocalSyncPath { get; set; } = account.SyncConfig?.LocalSyncPath.Value ?? string.Empty;

    [ObservableProperty]
    public partial ConflictPolicy ConflictPolicy { get; set; } = account.SyncConfig?.ConflictPolicy ?? ConflictPolicy.Ignore;

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; } =
    [
        new(ConflictPolicy.Ignore,        "Ignore",          "Skip conflicts — leave both unchanged"),
        new(ConflictPolicy.KeepBoth,      "Keep both",       "Rename local, keep remote"),
        new(ConflictPolicy.LastWriteWins, "Last write wins", "Most recently modified wins"),
        new(ConflictPolicy.LocalWins,     "Local wins",      "Local always overwrites remote"),
        new(ConflictPolicy.RemoteWins,    "Remote wins",     "Remote always overwrites local"),
    ];

    [RelayCommand]
    private static async Task BrowseAsync()
    {
        // Folder picker — wired via code-behind in SettingsView
        // to avoid taking a platform dependency in the ViewModel
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var resolvedPath = LocalSyncPathFactory.Create(LocalSyncPath).Match<Domain.LocalSyncPath?>(p => p, _ => null);
        account.SyncConfig = resolvedPath is null ? null : AccountSyncConfigFactory.Create(ConflictPolicy, resolvedPath);

        await repository.GetByIdAsync(account.Id, CancellationToken.None)
            .TapAsync(async entity =>
            {
                entity.SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy, resolvedPath ?? Domain.LocalSyncPath.Restore(string.Empty));
                await repository.UpsertAsync(entity, CancellationToken.None);
            });
    }
}
