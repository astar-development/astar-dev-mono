using System.Collections.ObjectModel;
using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.OneDriveSyncClient.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Home;

public sealed partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IGraphService graphService;
    private readonly Func<CancellationToken, Task<string>> tokenFactory;
    private readonly DriveId driveId;
    private readonly Func<string, FolderSyncState?> ruleStateResolver;
    private readonly Func<string, IReadOnlyList<string>> localFolderLister;
    private readonly ILogger<FolderTreeNodeViewModel> logger;
    private readonly ILocalizationService loc;
    private bool childrenLoaded;

    public Option<string> RemoteId { get; }
    public string Name { get; }
    public string? ParentId { get; }
    public string RemotePath { get; }
    public int Depth { get; }

    public bool IsLocalOnly => RemoteId is Option<string>.None;

    internal bool NeedsRulePersisted { get; }

    public string LocalOnlyBadgeText => loc.GetLocal("Files.FolderStatus.LocalOnly");

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
        FolderSyncState.Synced => loc.GetLocal("Files.FolderStatus.Synced"),
        FolderSyncState.Syncing => loc.GetLocal("Files.FolderStatus.Syncing"),
        FolderSyncState.Partial => loc.GetLocal("Files.FolderStatus.Partial"),
        FolderSyncState.Conflict => loc.GetLocal("Files.FolderStatus.Conflict"),
        FolderSyncState.Error => loc.GetLocal("Files.FolderStatus.Error"),
        _ => loc.GetLocal("Files.FolderStatus.Excluded")
    };

    public string ToggleLabel => loc.GetLocal(IsIncluded ? "Files.Exclude" : "Files.Include");
    public string ToggleTooltip => loc.GetLocal(IsIncluded ? "Files.Exclude.Tooltip" : "Files.Include.Tooltip", Name);

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
    public event EventHandler<FolderTreeNodeViewModel>? LocalOnlyFolderDiscovered;
    public event EventHandler<FolderTreeNodeViewModel>? OpenInFileManagerRequested;
    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    public FolderTreeNodeViewModel(FolderTreeNode node, IGraphService graphService, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver, Func<string, IReadOnlyList<string>> localFolderLister, ILogger<FolderTreeNodeViewModel> logger, ILocalizationService localizationService, int depth = 0)
    {
        RemoteId = node.RemoteId;
        Name = node.Name;
        ParentId = node.ParentId.Match<string?>(id => id, () => null);
        RemotePath = node.RemotePath;
        Depth = depth;
        SyncState = node.SyncState;
        HasChildren = node.HasChildren;
        this.graphService = graphService;
        this.tokenFactory = tokenFactory;
        this.driveId = driveId;
        this.ruleStateResolver = ruleStateResolver;
        this.localFolderLister = localFolderLister;
        this.logger = logger;
        loc = localizationService;
        NeedsRulePersisted = IsLocalOnly && ruleStateResolver(node.RemotePath) is null;
        loc.CultureChanged += OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        _ = ToggleLabel;
        _ = ToggleTooltip;
        OnPropertyChanged(nameof(ToggleLabel));
        OnPropertyChanged(nameof(ToggleTooltip));
        OnPropertyChanged(nameof(LocalOnlyBadgeText));
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
        if (childrenLoaded) return;

        IsLoadingChildren = true;
        try
        {
            var remoteFolders = await LoadRemoteChildFoldersAsync();

            if (remoteFolders is null)
                return;

            var mergedFolders = FolderTreeMerger.Merge(remoteFolders, localFolderLister(RemotePath), RemotePath, RemoteId, SyncState, path => ruleStateResolver(path).ToOption());

            Children.Clear();
            foreach (var mergedFolder in mergedFolders)
            {
                var childVm = CreateChildFolderTreeViewModel(MapMergedFolderToChildNode(mergedFolder));

                Children.Add(childVm);
            }

            if (Children.Count == 0) HasChildren = false;

            childrenLoaded = true;

            foreach (var child in Children.Where(child => child.NeedsRulePersisted))
                LocalOnlyFolderDiscovered?.Invoke(this, child);
        }
        finally
        {
            IsLoadingChildren = false;
        }
    }

    private async Task<List<DriveFolder>?> LoadRemoteChildFoldersAsync()
    {
        if (!RemoteId.TryGetValue(out string? remoteFolderId))
            return [];

        return await graphService.GetChildFoldersAsync(tokenFactory, driveId, remoteFolderId)
            .MatchAsync<List<DriveFolder>, string, List<DriveFolder>?>(
                f => f,
                error =>
                {
                    OneDriveSyncClientMessages.FolderChildrenLoadFailed(logger, RemotePath, error);
                    HasChildren = false;
                    return null;
                });
    }

    private FolderTreeNodeViewModel CreateChildFolderTreeViewModel(FolderTreeNode childNode)
    {
        var childVm = new FolderTreeNodeViewModel(childNode, graphService, tokenFactory, driveId, ruleStateResolver, localFolderLister, logger, loc, Depth + 1);

        childVm.IncludeToggled += (s, e) => IncludeToggled?.Invoke(s, e);
        childVm.LocalOnlyFolderDiscovered += (s, e) => LocalOnlyFolderDiscovered?.Invoke(s, e);
        childVm.OpenInFileManagerRequested += (s, e) => OpenInFileManagerRequested?.Invoke(s, e);
        childVm.ViewActivityRequested += (s, e) => ViewActivityRequested?.Invoke(s, e);

        return childVm;
    }

    private static FolderTreeNode MapMergedFolderToChildNode(MergedFolder mergedFolder)
        => new(mergedFolder.RemoteId, mergedFolder.Name, mergedFolder.ParentId, AccountId: string.Empty, RemotePath: mergedFolder.RemotePath, mergedFolder.SyncState, HasChildren: true);
}
