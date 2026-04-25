using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AnotherOneDriveSync.Core;
using AnotherOneDriveSync.Data;
using AnotherOneDriveSync.Data.Entities;
using ReactiveUI;
using Serilog;

namespace AnotherOneDriveSync.App.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IAuthService _authService;
    private readonly IGraphService _graphService;
    private readonly ISyncService _syncService;
    private readonly SyncDbContext _dbContext;
    private readonly ILogger _logger;

    private bool _isLoggedIn;
    private bool _isBusy;
    private string _statusMessage = "Ready";
    private string _localRootPath;
    private OneDriveTreeNodeViewModel? _selectedTreeNode;
    private SyncFolderItemViewModel? _selectedSyncFolder;

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set => this.RaiseAndSetIfChanged(ref _isLoggedIn, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string LocalRootPath
    {
        get => _localRootPath;
        set => this.RaiseAndSetIfChanged(ref _localRootPath, value);
    }

    public OneDriveTreeNodeViewModel? SelectedTreeNode
    {
        get => _selectedTreeNode;
        set => this.RaiseAndSetIfChanged(ref _selectedTreeNode, value);
    }

    public SyncFolderItemViewModel? SelectedSyncFolder
    {
        get => _selectedSyncFolder;
        set => this.RaiseAndSetIfChanged(ref _selectedSyncFolder, value);
    }

    public ObservableCollection<OneDriveTreeNodeViewModel> TreeRoots { get; } = new();
    public ObservableCollection<SyncFolderItemViewModel> SyncFolders { get; } = new();

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LoginCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RefreshCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AddFolderCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RemoveFolderCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SyncNowCommand { get; }

    public MainWindowViewModel(
        IAuthService authService,
        IGraphService graphService,
        ISyncService syncService,
        SyncDbContext dbContext,
        ILogger logger)
    {
        _authService = authService;
        _graphService = graphService;
        _syncService = syncService;
        _dbContext = dbContext;
        _logger = logger;

        var savedSettings = _dbContext.AppSettings.FirstOrDefault();
        _localRootPath = savedSettings?.LocalRootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive");

        this.WhenAnyValue(x => x.LocalRootPath)
            .Skip(1)
            .Subscribe(path => SaveLocalRootPath(path));

        foreach (var folder in _dbContext.SyncFolders.ToList())
            SyncFolders.Add(new SyncFolderItemViewModel(folder));

        var canAddFolder = this.WhenAnyValue(x => x.SelectedTreeNode)
            .Select(n => n != null && n.IsFolder);

        var canRemoveFolder = this.WhenAnyValue(x => x.SelectedSyncFolder)
            .Select(f => f != null);

        LoginCommand = ReactiveCommand.CreateFromTask(ExecuteLoginAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(ExecuteRefreshAsync);
        AddFolderCommand = ReactiveCommand.CreateFromTask(ExecuteAddFolderAsync, canAddFolder);
        RemoveFolderCommand = ReactiveCommand.CreateFromTask(ExecuteRemoveFolderAsync, canRemoveFolder);
        SyncNowCommand = ReactiveCommand.CreateFromTask(ExecuteSyncNowAsync);
    }

    private void SaveLocalRootPath(string path)
    {
        var settings = _dbContext.AppSettings.FirstOrDefault();
        if (settings == null)
        {
            _dbContext.AppSettings.Add(new Data.Entities.AppSettings { LocalRootPath = path });
        }
        else
        {
            settings.LocalRootPath = path;
            _dbContext.AppSettings.Update(settings);
        }
        _dbContext.SaveChanges();
        _logger.Information("LocalRootPath saved: {Path}", path);
    }

    private async Task ExecuteLoginAsync()
    {
        IsBusy = true;
        StatusMessage = "Logging in...";
        try
        {
            await _authService.AcquireTokenAsync();
            IsLoggedIn = true;
            StatusMessage = "Logged in";
            _logger.Information("Login successful");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Login failed: {ex.Message}";
            _logger.Error(ex, "Login failed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteRefreshAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading OneDrive...";
        try
        {
            TreeRoots.Clear();
            await foreach (var item in _graphService.ListDriveRootChildrenAsync())
            {
                TreeRoots.Add(new OneDriveTreeNodeViewModel(
                    item.Name ?? string.Empty,
                    item.Name ?? string.Empty,
                    item.Folder != null,
                    item.Id ?? string.Empty,
                    item.ParentReference?.DriveId ?? string.Empty,
                    _graphService));
            }
            StatusMessage = "Ready";
            _logger.Information("OneDrive tree refreshed, {Count} root items", TreeRoots.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading OneDrive: {ex.Message}";
            _logger.Error(ex, "Failed to refresh OneDrive tree");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteAddFolderAsync()
    {
        var node = SelectedTreeNode;
        if (node == null || !node.IsFolder)
            return;

        var existing = _dbContext.SyncFolders.FirstOrDefault(f => f.FolderId == node.ItemId);
        if (existing != null)
        {
            StatusMessage = $"{node.Name} already in sync list";
            return;
        }

        var syncFolder = new SyncFolder
        {
            DriveId = node.DriveId,
            FolderId = node.ItemId,
            LocalPath = node.Path
        };

        _dbContext.SyncFolders.Add(syncFolder);
        await _dbContext.SaveChangesAsync();
        SyncFolders.Add(new SyncFolderItemViewModel(syncFolder));
        StatusMessage = $"Added {node.Name} to sync list";
        _logger.Information("Added sync folder {Name}", node.Name);
    }

    private async Task ExecuteRemoveFolderAsync()
    {
        var item = SelectedSyncFolder;
        if (item == null)
            return;

        var entity = await _dbContext.SyncFolders.FindAsync(item.Id);
        if (entity != null)
        {
            _dbContext.SyncFolders.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        SyncFolders.Remove(item);
        SelectedSyncFolder = null;
        StatusMessage = $"Removed {item.DisplayName}";
        _logger.Information("Removed sync folder {Name}", item.DisplayName);
    }

    private async Task ExecuteSyncNowAsync()
    {
        if (SyncFolders.Count == 0)
        {
            StatusMessage = "No folders to sync";
            return;
        }

        IsBusy = true;
        try
        {
            var folders = SyncFolders.ToList();
            foreach (var folder in folders)
            {
                var entity = await _dbContext.SyncFolders.FindAsync(folder.Id);
                if (entity == null)
                    continue;

                StatusMessage = $"Syncing {folder.DisplayName}...";
                _logger.Information("Syncing folder {Name}", folder.DisplayName);
                await _syncService.SyncFolderAsync(entity, LocalRootPath);
            }
            StatusMessage = $"Sync complete at {DateTime.Now:HH:mm:ss}";
            _logger.Information("Sync complete");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync failed: {ex.Message}";
            _logger.Error(ex, "Sync failed");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
