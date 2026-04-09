using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed partial class AccountSyncSettingsViewModel(
    OneDriveAccount account,
    IAccountRepository repository) : ObservableObject
{
    public string AccountId => account.Id;
    public string Email => account.Email;
    public string DisplayName => account.DisplayName;
    public string AccentHex => AccountCardViewModel.PaletteHex(account.AccentIndex);

    [ObservableProperty]
    public partial string LocalSyncPath { get; set; } = account.LocalSyncPath;

    [ObservableProperty]
    public partial ConflictPolicy ConflictPolicy { get; set; } = account.ConflictPolicy;
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
        account.LocalSyncPath = LocalSyncPath;
        account.ConflictPolicy = ConflictPolicy;

        var entity = await repository.GetByIdAsync(account.Id, CancellationToken.None);
        if(entity is null)
            return;

        entity.LocalSyncPath = LocalSyncPath;
        entity.ConflictPolicy = ConflictPolicy;
        await repository.UpsertAsync(entity, CancellationToken.None);
    }
}
