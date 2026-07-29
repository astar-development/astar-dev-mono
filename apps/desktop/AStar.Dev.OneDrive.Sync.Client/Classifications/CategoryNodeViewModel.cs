using System.Collections.ObjectModel;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Represents a category node in the file classification hierarchy tree.</summary>
[ExcludeFromCodeCoverage]
public sealed partial class CategoryNodeViewModel : ObservableObject
{
    private readonly IFileClassificationRepository repository;
    private readonly ICategoryEditDialogService categoryEditDialogService;
    private readonly Action<CategoryNodeViewModel> onDeleteSelf;
    private readonly Option<FileClassificationCategoryId> parentId;
    private readonly IReadOnlyList<CategoryNodeViewModel> allCategories;
    private readonly Func<Task> reloadAsync;
    private readonly List<CategoryNodeViewModel?> parentOptionCandidates = [];
    private bool originalIsFamous;
    private bool originalIsInternet;
    private bool originalIncludeInSearch;

    public CategoryNodeViewModel(FileClassificationCategoryId categoryId, string name, int level, bool isFamous, bool isInternet, Option<FileClassificationCategoryId> parentId, bool includeInSearch, IFileClassificationRepository repository, ICategoryEditDialogService categoryEditDialogService, Action<CategoryNodeViewModel> onDeleteSelf, IReadOnlyList<CategoryNodeViewModel> allCategories, Func<Task> reloadAsync, string ancestorPath = "")
    {
        CategoryId = categoryId;
        Name = name;
        Level = level;
        IsFamous = isFamous;
        IsInternet = isInternet;
        IncludeInSearch = includeInSearch;
        AncestorPath = ancestorPath;
        this.parentId = parentId;
        this.repository = repository;
        this.categoryEditDialogService = categoryEditDialogService;
        this.onDeleteSelf = onDeleteSelf;
        this.allCategories = allCategories;
        this.reloadAsync = reloadAsync;

        Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsLeafNode));
    }

    /// <summary>The database identifier for this category.</summary>
    public FileClassificationCategoryId CategoryId { get; }

    /// <summary>Display name of this category.</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>Hierarchy level (1 = root, 2 = child, 3 = grandchild).</summary>
    public int Level { get; }

    /// <summary>Left indent, in pixels, applied when this node is rendered in the flattened category list.</summary>
    public double IndentWidth => (Level - 1) * 20;

    /// <summary>Names of this node's ancestors, joined with " &gt; ", excluding this node itself. Empty for root categories. Used to disambiguate hierarchy in the flattened list, since the row directly above may not be this node's real parent when a search filter is active.</summary>
    public string AncestorPath { get; }

    /// <summary>True when <see cref="AncestorPath"/> is non-empty and should be displayed.</summary>
    public bool HasAncestorPath => AncestorPath.Length > 0;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(ExpandCollapseGlyph));

    /// <summary>Chevron glyph reflecting <see cref="IsExpanded"/>, for the expand/collapse toggle button.</summary>
    public string ExpandCollapseGlyph => IsExpanded ? "▾" : "▸";

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial bool IncludeInSearch { get; set; }

    [ObservableProperty]
    public partial string EditedName { get; set; } = string.Empty;

    /// <summary>Child categories nested under this node.</summary>
    public ObservableCollection<CategoryNodeViewModel> Children { get; } = [];

    /// <summary>True when this node has no children and can therefore hold keywords.</summary>
    public bool IsLeafNode => Children.Count == 0;

    /// <summary>Display names of the categories this node can be reparented under, with a leading "no parent" option. Populated when editing starts.</summary>
    public ObservableCollection<string> ParentOptionNames { get; } = [];

    /// <summary><see cref="ParentOptionNames"/> narrowed by <see cref="ParentFilterText"/>, for the parent picker's ComboBox.</summary>
    public ObservableCollection<string> FilteredParentOptionNames { get; } = [];

    [ObservableProperty]
    public partial string ParentFilterText { get; set; } = string.Empty;
    partial void OnParentFilterTextChanged(string value) => RefreshFilteredParentOptionNames();

    [ObservableProperty]
    public partial int SelectedParentOptionIndex { get; set; }
    partial void OnSelectedParentOptionIndexChanged(int value) => OnPropertyChanged(nameof(SelectedParentOptionName));

    /// <summary>Name of the currently-selected parent option, for binding to a filtered ComboBox's SelectedItem.</summary>
    public string? SelectedParentOptionName
    {
        get => ParentOptionNames.ElementAtOrDefault(SelectedParentOptionIndex);
        set
        {
            int matchingIndex = ParentOptionNames.IndexOf(value ?? string.Empty);
            if (matchingIndex >= 0)
                SelectedParentOptionIndex = matchingIndex;
        }
    }

    [ObservableProperty]
    public partial bool IsFamous { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddKeywordCommand))]
    public partial bool IsInternet { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddKeywordCommand))]
    public partial string NewKeyword { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddChildCategoryCommand))]
    public partial string NewChildCategoryName { get; set; } = string.Empty;

    [RelayCommand]
    private async Task EditAsync()
    {
        EditedName = Name;
        originalIsFamous = IsFamous;
        originalIsInternet = IsInternet;
        originalIncludeInSearch = IncludeInSearch;
        ParentFilterText = string.Empty;
        RefreshParentOptions();
        IsEditing = true;

        await categoryEditDialogService.ShowAsync(this).ConfigureAwait(false);
    }

    private void RefreshParentOptions()
    {
        var selfAndDescendants = SelfAndDescendants();
        List<CategoryNodeViewModel> eligibleParents = [.. allCategories.Where(category => !selfAndDescendants.Contains(category))];

        parentOptionCandidates.Clear();
        parentOptionCandidates.Add(null);
        parentOptionCandidates.AddRange(eligibleParents);

        ParentOptionNames.Clear();
        ParentOptionNames.Add("(No parent - root)");
        foreach (var candidate in eligibleParents)
            ParentOptionNames.Add(candidate.Name);

        int currentParentIndex = parentId is Option<FileClassificationCategoryId>.Some someParent
            ? parentOptionCandidates.FindIndex(candidate => candidate is not null && candidate.CategoryId.Equals(someParent.Value))
            : 0;
        SelectedParentOptionIndex = currentParentIndex >= 0 ? currentParentIndex : 0;

        RefreshFilteredParentOptionNames();
    }

    private void RefreshFilteredParentOptionNames()
    {
        FilteredParentOptionNames.Clear();
        foreach (string name in ParentOptionNames.Where(name => name.Contains(ParentFilterText, StringComparison.OrdinalIgnoreCase)))
            FilteredParentOptionNames.Add(name);
    }

    private HashSet<CategoryNodeViewModel> SelfAndDescendants()
    {
        HashSet<CategoryNodeViewModel> collected = [];
        void Collect(CategoryNodeViewModel node)
        {
            collected.Add(node);
            foreach (var child in node.Children)
                Collect(child);
        }
        Collect(this);

        return collected;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        string trimmedName = EditedName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            IsEditing = false;
            return;
        }

        await FileClassificationCategoryFactory.Create(CategoryId, trimmedName, Level, IsFamous, IsInternet, parentId, IncludeInSearch)
            .Match(category => PersistUpdateAsync(category, trimmedName), _ => Task.CompletedTask)
            .ConfigureAwait(false);

        var selectedParentId = SelectedParentId();
        if (!selectedParentId.Equals(parentId))
            await ReparentAsync(selectedParentId).ConfigureAwait(false);
    }

    private Option<FileClassificationCategoryId> SelectedParentId()
    {
        var selected = parentOptionCandidates.ElementAtOrDefault(SelectedParentOptionIndex);

        return selected is null ? Option.None<FileClassificationCategoryId>() : Option.Some(selected.CategoryId);
    }

    private async Task ReparentAsync(Option<FileClassificationCategoryId> newParentId)
        => await repository.ReparentCategoryAsync(CategoryId, newParentId, CancellationToken.None)
            .MatchAsync(async _ =>
            {
                await reloadAsync().ConfigureAwait(false);

                return true;
            }, _ => Task.FromResult(false))
            .ConfigureAwait(false);

    private async Task PersistUpdateAsync(FileClassificationCategory category, string trimmedName)
        => await repository.UpdateCategoryAsync(CategoryId, category, CancellationToken.None)
            .Tap(_ =>
            {
                Name = trimmedName;
                IsEditing = false;
            })
            .ConfigureAwait(false);

    [RelayCommand]
    private void Cancel()
    {
        EditedName = string.Empty;
        IsFamous = originalIsFamous;
        IsInternet = originalIsInternet;
        IncludeInSearch = originalIncludeInSearch;
        IsEditing = false;
    }

    private bool CanAddKeyword => !string.IsNullOrWhiteSpace(NewKeyword) && Children.Count == 0;

    [RelayCommand(CanExecute = nameof(CanAddKeyword))]
    private Task AddKeywordAsync() => Task.CompletedTask;

    private bool CanAddCategory => !string.IsNullOrWhiteSpace(NewChildCategoryName);

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private async Task AddChildCategoryAsync()
    {
        int childLevel = Level + 1;
        var placeholder = new FileClassificationCategoryId(0);
        string trimmedName = NewChildCategoryName.Trim().ToTitleCase();

        await FileClassificationCategoryFactory.Create(placeholder, trimmedName, childLevel, IsFamous, IsInternet, Option.Some(CategoryId), IncludeInSearch)
            .Match(category => AddValidatedChildCategoryAsync(category, trimmedName, childLevel), _ => Task.CompletedTask)
            .ConfigureAwait(false);
    }

    private async Task AddValidatedChildCategoryAsync(FileClassificationCategory category, string trimmedName, int childLevel)
    {
        string childAncestorPath = HasAncestorPath ? $"{AncestorPath} > {Name}" : Name;

        await repository.AddCategoryAsync(category, CancellationToken.None)
            .Tap(newId =>
            {
                var newChild = new CategoryNodeViewModel(newId, trimmedName, childLevel, IsFamous, IsInternet, Option.Some(CategoryId), IncludeInSearch, repository, categoryEditDialogService, self => Children.Remove(self), allCategories, reloadAsync, childAncestorPath);
                Children.Add(newChild);
                IsExpanded = true;
            })
            .ConfigureAwait(false);

        NewChildCategoryName = string.Empty;
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private async Task DeleteSelfAsync()
    {
        await repository.DeleteCategoryAsync(CategoryId, CancellationToken.None).ConfigureAwait(false);
        onDeleteSelf(this);
    }
}
