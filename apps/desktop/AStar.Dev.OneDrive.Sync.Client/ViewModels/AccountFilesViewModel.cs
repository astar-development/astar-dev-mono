using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class AccountFilesViewModel(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository) : ObservableObject
{
    private readonly OneDriveAccount    _account      = account;
    private readonly IAuthService       _authService  = authService;
    private readonly IGraphService      _graphService = graphService;
    private readonly IAccountRepository _repository   = repository;
    private          string?            _accessToken;
    private          string?            _driveId;

    public string AccountId => _account.Id;
    public string DisplayName => _account.DisplayName;
    public string Email => _account.Email;

    public string TabLabel => _account.DisplayName.Length > 0
                                 ? _account.DisplayName
                                 : _account.Email;

    public int AccentIndex => _account.AccentIndex;

    public Color AccentColor => AccountCardViewModel.PaletteColor(_account.AccentIndex);

    public ObservableCollection<FolderTreeNodeViewModel> RootFolders { get; } = [];

    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private string _loadError    = string.Empty;
    [ObservableProperty] private bool   _hasLoadError;

    [ObservableProperty] private bool _isActiveTab;

    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsLoading)
            return;

        IsLoading = true;
        HasLoadError = false;
        LoadError = string.Empty;
        RootFolders.Clear();

        try
        {
            var authResult = await _authService.AcquireTokenSilentAsync(_account.Id);

            if(authResult.IsError)
            {
                LoadError = authResult.ErrorMessage ?? "Authentication failed.";
                HasLoadError = true;
                return;
            }

            _accessToken = authResult.AccessToken!;

            _driveId = await _graphService.GetDriveIdAsync(_accessToken);

            var folders = await _graphService.GetRootFoldersAsync(_accessToken);

            foreach(var f in folders)
            {
                var syncState = _account.SelectedFolderIds.Contains(f.Id)
                    ? FolderSyncState.Included
                    : FolderSyncState.Excluded;

                var node = new FolderTreeNode(
                    Id:          f.Id,
                    Name:        f.Name,
                    ParentId:    f.ParentId,
                    AccountId:   _account.Id,
                    SyncState:   syncState,
                    HasChildren: true);

                var vm = new FolderTreeNodeViewModel(
                    node, _graphService, _accessToken, _driveId);

                vm.IncludeToggled += OnIncludeToggled;
                vm.ViewActivityRequested += OnViewActivityRequested;
                vm.OpenInFileManagerRequested += OnOpenInFileManager;

                RootFolders.Add(vm);
            }
        }
        catch(Exception ex)
        {
            LoadError = $"Failed to load folders: {ex.Message}";
            HasLoadError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnIncludeToggled(object? sender, FolderTreeNodeViewModel node)
    {
        if(node.IsIncluded)
        {
            if(!_account.SelectedFolderIds.Contains(node.Id))
                _account.SelectedFolderIds.Add(node.Id);
        }
        else
        {
            _ = _account.SelectedFolderIds.Remove(node.Id);
        }

        var entity = new AccountEntity
        {
            Id           = _account.Id,
            DisplayName  = _account.DisplayName,
            Email        = _account.Email,
            AccentIndex  = _account.AccentIndex,
            IsActive     = _account.IsActive,
            DeltaLink    = _account.DeltaLink,
            LastSyncedAt = _account.LastSyncedAt,
            QuotaTotal   = _account.QuotaTotal,
            QuotaUsed    = _account.QuotaUsed,
            SyncFolders  = [.. RootFolders
                .Where(f => f.IsIncluded)
                .Select(f => new SyncFolderEntity
                {
                    FolderId   = f.Id,
                    FolderName = f.Name,
                    AccountId  = _account.Id
                })]
        };

        await _repository.UpsertAsync(entity);
    }

    private void OnViewActivityRequested(object? sender, FolderTreeNodeViewModel node)
        => ViewActivityRequested?.Invoke(this, node);

    private static void OnOpenInFileManager(object? sender, FolderTreeNodeViewModel node)
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", node.Name);

        if(!Directory.Exists(path))
            return;

        string opener = OperatingSystem.IsWindows() ? "explorer"
                   : OperatingSystem.IsMacOS()   ? "open"
                   : "xdg-open";

        _ = System.Diagnostics.Process.Start(opener, path);
    }
}
