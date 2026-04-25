using System.Collections.ObjectModel;
using AnotherOneDriveSync.Core;
using ReactiveUI;

namespace AnotherOneDriveSync.App.ViewModels;

public class OneDriveTreeNodeViewModel : ReactiveObject
{
    private readonly IGraphService _graphService;
    private bool _isExpanded;
    private bool _isLoading;
    private bool _childrenLoaded;

    public string Name { get; }
    public string Path { get; }
    public bool IsFolder { get; }
    public string ItemId { get; }
    public string DriveId { get; }
    public ObservableCollection<OneDriveTreeNodeViewModel> Children { get; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            this.RaiseAndSetIfChanged(ref _isExpanded, value);
            if (value && IsFolder && !_childrenLoaded)
                _ = LoadChildrenAsync();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public OneDriveTreeNodeViewModel(string name, string path, bool isFolder, string itemId, string driveId, IGraphService graphService)
    {
        Name = name;
        Path = path;
        IsFolder = isFolder;
        ItemId = itemId;
        DriveId = driveId;
        _graphService = graphService;

        if (isFolder)
            Children.Add(new OneDriveTreeNodeViewModel("Loading...", string.Empty, false, string.Empty, string.Empty, graphService));
    }

    private async Task LoadChildrenAsync()
    {
        _childrenLoaded = true;
        IsLoading = true;
        try
        {
            var items = new List<OneDriveTreeNodeViewModel>();
            await foreach (var item in _graphService.ListFolderChildrenAsync(ItemId))
            {
                var childPath = string.IsNullOrEmpty(Path) ? item.Name : $"{Path}/{item.Name}";
                items.Add(new OneDriveTreeNodeViewModel(
                    item.Name ?? string.Empty,
                    childPath ?? string.Empty,
                    item.Folder != null,
                    item.Id ?? string.Empty,
                    item.ParentReference?.DriveId ?? DriveId,
                    _graphService));
            }

            Children.Clear();
            foreach (var item in items)
                Children.Add(item);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
