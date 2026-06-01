using System.Collections.ObjectModel;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IGraphService _graphService;
    private readonly Func<CancellationToken, Task<string>> _tokenFactory;
    private readonly DriveId _driveId;
    private readonly ILogger<FolderTreeNodeViewModel> _logger;
    private readonly ILocalizationService loc;
    private bool _childrenLoaded;

    public string Id { get; }
    public string Name { get; }
    public string? ParentId { get; }
    public string RemotePath { get; }
    public int Depth { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncluded))]
    [NotifyPropertyChangedFor(nameof(IsExcluded))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeText))]
    [NotifyPropertyChangedFor(nameof(ToggleLabel))]
    [NotifyPropertyChangedFor(nameof(ToggleTooltip))]
    public partial FolderSyncState SyncState { get; set; }

    public bool IsIncluded => SyncState is not FolderSyncState.Excluded;
    public bool IsExcluded => SyncState is FolderSyncState.Excluded;

    public string StatusBadgeText => SyncState switch
    {
        FolderSyncState.Included => loc.GetLocal("Files.FolderStatus.Included"),
        FolderSyncState.Synced   => loc.GetLocal("Files.FolderStatus.Synced"),
        FolderSyncState.Syncing  => loc.GetLocal("Files.FolderStatus.Syncing"),
        FolderSyncState.Partial  => loc.GetLocal("Files.FolderStatus.Partial"),
        FolderSyncState.Conflict => loc.GetLocal("Files.FolderStatus.Conflict"),
        FolderSyncState.Error    => loc.GetLocal("Files.FolderStatus.Error"),
        _                        => loc.GetLocal("Files.FolderStatus.Excluded")
    };

    public string ToggleLabel => loc.GetLocal(IsIncluded ? "Files.Exclude" : "Files.Include");
    public string ToggleTooltip => loc.GetLocal(IsIncluded ? "Files.Exclude.Tooltip" : "Files.Include.Tooltip");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderGlyph))]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingChildren { get; set; }

    [ObservableProperty]
    public partial bool HasChildren { get; set; }

    public string ExpanderGlyph => IsExpanded ? "▾" : "▸";

    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public event EventHandler<FolderTreeNodeViewModel>? IncludeToggled;
    public event EventHandler<FolderTreeNodeViewModel>? OpenInFileManagerRequested;
    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    public FolderTreeNodeViewModel(FolderTreeNode node, IGraphService graphService, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, ILogger<FolderTreeNodeViewModel> logger, ILocalizationService localizationService, int depth = 0)
    {
        Id = node.Id;
        Name = node.Name;
        ParentId = node.ParentId.Match<string?>(id => id, () => null);
        RemotePath = node.RemotePath;
        Depth = depth;
        SyncState = node.SyncState;
        HasChildren = node.HasChildren;
        _graphService = graphService;
        _tokenFactory = tokenFactory;
        _driveId = driveId;
        _logger = logger;
        loc = localizationService;
        loc.CultureChanged += OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        _ = ToggleLabel;
        _ = ToggleTooltip;
        OnPropertyChanged(nameof(ToggleLabel));
        OnPropertyChanged(nameof(ToggleTooltip));
    }

    [RelayCommand]
    private async Task ToggleExpandAsync()
    {
        if (!HasChildren)
            return;

        if (!IsExpanded)
        {
            await EnsureChildrenLoadedAsync();
            IsExpanded = true;
        }
        else
        {
            IsExpanded = false;
        }
    }

    [RelayCommand]
    private void ToggleInclude()
    {
        var newState = SyncState is FolderSyncState.Excluded
            ? FolderSyncState.Included
            : FolderSyncState.Excluded;

        ApplySyncStateRecursively(newState);
        IncludeToggled?.Invoke(this, this);
    }

    internal void ApplySyncStateRecursively(FolderSyncState state)
    {
        SyncState = state;

        foreach (var child in Children)
            child.ApplySyncStateRecursively(state);
    }

    [RelayCommand]
    private void OpenInFileManager()
        => OpenInFileManagerRequested?.Invoke(this, this);

    [RelayCommand]
    private void ViewActivity()
        => ViewActivityRequested?.Invoke(this, this);

    private async Task EnsureChildrenLoadedAsync()
    {
        if (_childrenLoaded) return;

        IsLoadingChildren = true;
        try
        {
            var folders = await _graphService.GetChildFoldersAsync(_tokenFactory, _driveId, Id)
                .MatchAsync<List<DriveFolder>, string, List<DriveFolder>?>(
                    f => f,
                    error =>
                    {
                        OneDriveSyncClientMessages.FolderChildrenLoadFailed(_logger, RemotePath, error);
                        HasChildren = false;
                        return null;
                    });

            if (folders is null)
                return;

            Children.Clear();
            foreach (var f in folders)
            {
                var childVm = CreateChildFolderTreeViewModel(f);

                Children.Add(childVm);
            }

            if (Children.Count == 0) HasChildren = false;

            _childrenLoaded = true;
        }
        finally
        {
            IsLoadingChildren = false;
        }
    }

    private FolderTreeNodeViewModel CreateChildFolderTreeViewModel(DriveFolder f)
    {
        string childRemotePath = $"{RemotePath}/{f.Name}";
        var childNode = MapDriveFolderToChildNode(f, childRemotePath);

        return CreateChildFolderTreeViewModel(childNode);
    }

    private FolderTreeNodeViewModel CreateChildFolderTreeViewModel(FolderTreeNode childNode)
    {
        var childVm = new FolderTreeNodeViewModel(childNode, _graphService, _tokenFactory, _driveId, _logger, loc, Depth + 1);

        childVm.IncludeToggled += (s, e) => IncludeToggled?.Invoke(s, e);
        childVm.OpenInFileManagerRequested += (s, e) => OpenInFileManagerRequested?.Invoke(s, e);
        childVm.ViewActivityRequested += (s, e) => ViewActivityRequested?.Invoke(s, e);

        return childVm;
    }

    private FolderTreeNode MapDriveFolderToChildNode(DriveFolder f, string childRemotePath)
        => new(f.Id, f.Name, f.ParentId, AccountId: string.Empty, RemotePath: childRemotePath, SyncState, HasChildren: true);
}
