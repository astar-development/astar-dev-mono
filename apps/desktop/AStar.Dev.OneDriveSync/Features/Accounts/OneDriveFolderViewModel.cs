using System.Collections.ObjectModel;
using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.FolderBrowsing;
using AStar.Dev.OneDriveSync.Infrastructure;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     View model for a single node in the OneDrive folder tree (AM-03).
///     Children are loaded lazily when <see cref="IsExpanded"/> is set to <c>true</c>.
/// </summary>
public sealed class OneDriveFolderViewModel : ViewModelBase
{
    private readonly IOneDriveFolderService _folderService;
    private readonly string _accessToken;
    private bool _childrenLoaded;

    public OneDriveFolderViewModel(OneDriveFolder folder, IOneDriveFolderService folderService, string accessToken)
    {
        _folderService = folderService;
        _accessToken   = accessToken;
        Id             = folder.Id;
        Name           = folder.Name;
        HasChildren    = folder.HasChildren;
        IsSelected     = true;

        LoadChildrenCommand = ReactiveCommand.CreateFromTask(LoadChildrenAsync);
    }

    /// <summary>OneDrive item ID.</summary>
    public string Id { get; }

    /// <summary>Display name for this folder.</summary>
    public string Name { get; }

    /// <summary>Whether this folder has any sub-folders (used to show expand icon).</summary>
    public bool HasChildren { get; }

    /// <summary>Whether this folder is selected for sync (default: true = all selected, AM-03).</summary>
    public bool IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Whether children are currently being loaded.</summary>
    public bool IsLoadingChildren
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Triggers lazy child loading on first expansion (AM-03).
    ///     Subscribers must call <c>.ObserveOn(RxApp.MainThreadScheduler)</c> before mutating bound collections.
    /// </summary>
    public bool IsExpanded
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            if (value && !_childrenLoaded && HasChildren)
                LoadChildrenCommand.Execute().Subscribe();
        }
    }

    public ObservableCollection<OneDriveFolderViewModel> Children { get; } = [];

    public ReactiveCommand<Unit, Unit> LoadChildrenCommand { get; }

    private async Task LoadChildrenAsync(CancellationToken ct)
    {
        if (_childrenLoaded)
            return;

        IsLoadingChildren = true;

        var result = await _folderService
            .GetChildFoldersAsync(_accessToken, Id, ct)
            .ConfigureAwait(false);

        result
            .Tap(folders =>
            {
                foreach (var folder in folders)
                    Children.Add(new OneDriveFolderViewModel(folder, _folderService, _accessToken));

                _childrenLoaded = true;
            });

        IsLoadingChildren = false;
    }
}
