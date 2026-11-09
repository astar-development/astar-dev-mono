using System.Collections.ObjectModel;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>One folder of the selection tree: its checkbox, its lazily loaded children and, for the top of a sync pair, its direction.</summary>
public sealed class FolderNodeViewModel : LocalizedObservableObject
{
    private readonly FolderTreeNode node;
    private readonly FolderSelectionViewModel owner;
    private bool isLoaded;
    private bool isExpanded;
    private bool isLoading;
    private string loadErrorText = string.Empty;

    internal FolderNodeViewModel(FolderTreeNode node, FolderSelectionViewModel owner, ILocalizationService localization) : base(localization)
    {
        this.node = node;
        this.owner = owner;
        ExpandCommand = new AsyncRelayCommand(ExpandAsync);
        ToggleCommand = new RelayCommand(() => owner.Toggle(this), () => CanToggle);
        ChangeDirectionCommand = new RelayCommand(() => owner.ChangeDirection(this), () => IsPairRoot);
    }

    /// <summary>The folder's name.</summary>
    public string Name => node.Name;

    /// <summary>The folder's path relative to the sync root.</summary>
    public LocalRelativePath Path => node.Path;

    /// <summary>The cloud folder's id, when it exists in the cloud.</summary>
    public Option<RemoteItemId> RemoteId => node.RemoteId;

    /// <summary>The loaded child folders.</summary>
    public ObservableCollection<FolderNodeViewModel> Children { get; } = [];

    /// <summary>Where the folder exists, in words.</summary>
    public string OriginText => Localization.GetLocal(node.Origin switch
    {
        FolderOrigin.Both => "FolderSelection.Origin.Both",
        FolderOrigin.RemoteOnly => "FolderSelection.Origin.RemoteOnly",
        _ => "FolderSelection.Origin.LocalOnly",
    });

    /// <summary>The checkbox state.</summary>
    public FolderCheckState CheckState { get; private set; } = FolderCheckStateFactory.CreateUnticked();

    /// <summary>The checkbox state in words, for screen readers.</summary>
    public string CheckStateText => Localization.GetLocal(CheckState switch
    {
        FolderCheckState.Ticked => "FolderSelection.Check.Ticked",
        FolderCheckState.PartiallyTicked => "FolderSelection.Check.Partial",
        _ => "FolderSelection.Check.Unticked",
    });

    /// <summary>The name for screen readers, with the checkbox state.</summary>
    public string AccessibleName => Localization.GetLocal("FolderSelection.Node.AccessibleName", node.Name, CheckStateText);

    /// <summary>Whether two local folders differ from this one only by case.</summary>
    public bool HasCaseClash => node.NameClash is FolderNameClash.CaseClash;

    /// <summary>The warning shown for a case clash, or empty.</summary>
    public string CaseClashText => HasCaseClash ? Localization.GetLocal("FolderSelection.CaseClash") : string.Empty;

    /// <summary>Whether the checkbox can be changed. A folder with a case clash cannot be ticked until it is renamed.</summary>
    public bool CanToggle => !HasCaseClash || CheckState is FolderCheckState.Ticked;

    /// <summary>Whether the folder may have child folders, so the tree shows an expand arrow.</summary>
    public bool MayHaveChildren => node.Children is FolderChildren.MayHaveChildFolders;

    /// <summary>Whether the folder is the top of a sync pair.</summary>
    public bool IsPairRoot { get; private set; }

    /// <summary>The pair's direction in words, or empty when the folder is not the top of a pair.</summary>
    public string DirectionText { get; private set; } = string.Empty;

    /// <summary>Whether the folder is open.</summary>
    public bool IsExpanded
    {
        get => isExpanded;
        set => SetProperty(ref isExpanded, value);
    }

    /// <summary>Whether the children are loading.</summary>
    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    /// <summary>Why the children could not be listed, or empty.</summary>
    public string LoadErrorText
    {
        get => loadErrorText;
        private set
        {
            if (SetProperty(ref loadErrorText, value))
                OnPropertyChanged(nameof(HasLoadError));
        }
    }

    /// <summary>Whether the children failed to load.</summary>
    public bool HasLoadError => loadErrorText.Length > 0;

    /// <summary>The retry button's text.</summary>
    public string RetryText => Localization.GetLocal("FolderSelection.Retry");

    /// <summary>The change-direction button's text.</summary>
    public string ChangeDirectionText => Localization.GetLocal("FolderSelection.Direction.Change");

    /// <summary>Opens the folder, loading its children the first time and after a failure.</summary>
    public IAsyncRelayCommand ExpandCommand { get; }

    /// <summary>Ticks or unticks the folder. This stages a change; nothing is saved until Apply.</summary>
    public IRelayCommand ToggleCommand { get; }

    /// <summary>Stages the next direction for the pair this folder tops.</summary>
    public IRelayCommand ChangeDirectionCommand { get; }

    /// <summary>Returns a redacted form, so folder names never reach logs by accident.</summary>
    public override string ToString() => "FolderNodeViewModel(redacted)";

    internal void Refresh(SelectionState state, SyncPairId ownPair, IReadOnlyList<SelectionRule> planned)
    {
        CheckState = state switch
        {
            SelectionState.Included => FolderCheckStateFactory.CreateTicked(),
            SelectionState.PartiallyIncluded => FolderCheckStateFactory.CreatePartiallyTicked(),
            _ => FolderCheckStateFactory.CreateUnticked(),
        };
        IsPairRoot = state is SelectionState.Included included && included.Pair == ownPair;
        DirectionText = state is SelectionState.Included { } direction && IsPairRoot ? Localization.GetLocal(DirectionKey(direction.Direction)) : string.Empty;
        OnPropertyChanged(nameof(CheckState));
        OnPropertyChanged(nameof(CheckStateText));
        OnPropertyChanged(nameof(AccessibleName));
        OnPropertyChanged(nameof(CanToggle));
        OnPropertyChanged(nameof(IsPairRoot));
        OnPropertyChanged(nameof(DirectionText));
        ToggleCommand.NotifyCanExecuteChanged();
        ChangeDirectionCommand.NotifyCanExecuteChanged();
        foreach (var child in Children)
            owner.RefreshNode(child, planned);
    }

    private static string DirectionKey(SyncDirection direction) => direction switch
    {
        SyncDirection.UploadOnly => "Direction.UploadOnly",
        SyncDirection.DownloadOnly => "Direction.DownloadOnly",
        _ => "Direction.TwoWay",
    };

    private async Task ExpandAsync(CancellationToken cancellationToken)
    {
        if (isLoaded)
        {
            IsExpanded = true;

            return;
        }

        IsLoading = true;
        var listed = await owner.ListAsync(FolderTreePositionFactory.CreateFolder(node.Path, node.RemoteId), cancellationToken);
        IsLoading = false;
        LoadErrorText = listed.Match(
            nodes =>
            {
                Children.Clear();
                foreach (var child in nodes)
                    Children.Add(owner.CreateNode(child));

                isLoaded = true;

                return string.Empty;
            },
            owner.FailureText);
        IsExpanded = true;
    }
}
