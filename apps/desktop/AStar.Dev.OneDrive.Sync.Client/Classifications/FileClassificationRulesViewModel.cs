using System.Collections;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>View model for the file classification rules tree view.</summary>
public sealed partial class FileClassificationRulesViewModel : ObservableObject
{
    private readonly IFileClassificationRepository repository;
    private readonly IFileClassificationExportImportService exportImportService;
    private readonly IFilePickerService filePickerService;
    private readonly IConfirmationDialogService confirmationDialogService;
    private readonly ICategoryEditDialogService categoryEditDialogService;
    private readonly ILocalizationService localizationService;
    private readonly IFileSystem fileSystem;

    public FileClassificationRulesViewModel(IFileClassificationRepository repository, IFileClassificationExportImportService exportImportService, IFilePickerService filePickerService, IConfirmationDialogService confirmationDialogService, ICategoryEditDialogService categoryEditDialogService, ILocalizationService localizationService, IFileSystem fileSystem)
    {
        this.repository = repository;
        this.exportImportService = exportImportService;
        this.filePickerService = filePickerService;
        this.confirmationDialogService = confirmationDialogService;
        this.categoryEditDialogService = categoryEditDialogService;
        this.localizationService = localizationService;
        this.fileSystem = fileSystem;
        Categories.CollectionChanged += (_, e) =>
        {
            OnPropertyChanged(nameof(HasNoCategories));
            AttachStructureChangeHandlers(e.NewItems);
            if (!suppressVisibleCategoriesRebuild)
                RebuildVisibleCategories();
        };
    }

    private bool suppressVisibleCategoriesRebuild;

    /// <summary>Root-level category nodes.</summary>
    public ObservableCollection<CategoryNodeViewModel> Categories { get; } = [];

    /// <summary>Flattened, preorder traversal of the category tree, used to render a single virtualized list instead of nesting nested trees of controls.</summary>
    public ObservableCollection<CategoryNodeViewModel> VisibleCategories { get; } = [];

    private void AttachStructureChangeHandlers(IList? nodes)
    {
        if (nodes is null)
            return;

        foreach (CategoryNodeViewModel node in nodes)
        {
            node.Children.CollectionChanged += (_, e) =>
            {
                AttachStructureChangeHandlers(e.NewItems);
                if (!suppressVisibleCategoriesRebuild)
                    RebuildVisibleCategories();
            };
            node.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CategoryNodeViewModel.IsExpanded) && !suppressVisibleCategoriesRebuild)
                    RebuildVisibleCategories();
            };
            AttachStructureChangeHandlers(node.Children);
        }
    }

    private void RebuildVisibleCategories()
    {
        VisibleCategories.Clear();
        foreach (var root in Categories)
            AppendVisible(root);
    }

    private void AppendVisible(CategoryNodeViewModel node)
    {
        bool filterTextActive = !string.IsNullOrWhiteSpace(FilterText);
        bool matchesFilterText = !filterTextActive || NodeOrDescendantMatchesFilterText(node);

        if (node.IncludeInSearch == ShowOnlyIncludedInSearch && matchesFilterText)
            VisibleCategories.Add(node);

        if (node.IsExpanded || (filterTextActive && matchesFilterText))
            foreach (var child in node.Children)
                AppendVisible(child);
    }

    private bool NodeOrDescendantMatchesFilterText(CategoryNodeViewModel node)
    {
        if (node.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var child in node.Children)
            if (NodeOrDescendantMatchesFilterText(child))
                return true;

        return false;
    }

    /// <summary>When true, <see cref="VisibleCategories"/> only includes categories where <see cref="CategoryNodeViewModel.IncludeInSearch"/> is true; when false, only categories where it is false. Display-only; does not change stored data.</summary>
    [ObservableProperty]
    public partial bool ShowOnlyIncludedInSearch { get; set; } = true;
    partial void OnShowOnlyIncludedInSearchChanged(bool value)
    {
        RebuildVisibleCategories();
        OnPropertyChanged(nameof(SearchFilterLabel));
    }

    /// <summary>Label describing the current state of the <see cref="ShowOnlyIncludedInSearch"/> filter toggle.</summary>
    public string SearchFilterLabel => ShowOnlyIncludedInSearch ? "Showing: included in search" : "Showing: excluded from search";

    /// <summary>Free-text filter narrowing <see cref="VisibleCategories"/> to nodes whose name, or a descendant's name, contains this value (case-insensitive). Ancestors of a match stay visible and force-expand regardless of <see cref="CategoryNodeViewModel.IsExpanded"/>.</summary>
    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;
    partial void OnFilterTextChanged(string value) => RebuildVisibleCategories();

    /// <summary>True when the view model is loading data from the repository.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    /// <summary>True when loading is complete and no categories have been loaded.</summary>
    public bool HasNoCategories => !IsLoading && Categories.Count == 0;

    /// <summary>True after the most recent successful <see cref="LoadAsync"/> completes.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Resets loading state synchronously so the view renders the loading indicator before the background load begins.</summary>
    public void PrepareForLoad()
    {
        IsLoaded = false;
        Categories.Clear();
        IsLoading = true;
    }

    /// <summary>Localised text to display while loading.</summary>
    public string LoadingText => localizationService.GetLocal("Common.Loading");

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
    public partial string NewCategoryName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
    public partial bool IsFamous { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
    public partial bool IsInternet { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
    public partial bool IncludeInSearch { get; set; }
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(HasNoCategories));

    /// <summary>Loads all categories from the repository and builds the tree, then populates keywords for each leaf node.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            var all = await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);

            var nodeDict = new Dictionary<FileClassificationCategoryId, CategoryNodeViewModel>();

            foreach (var category in all.OrderBy(c => c.Level).ThenBy(c => c.Name))
            {
                string ancestorPath = category.ParentId is Option<FileClassificationCategoryId>.Some someParent && nodeDict.TryGetValue(someParent.Value, out var parentNode)
                    ? HasParentPath(parentNode)
                    : string.Empty;

                var node = new CategoryNodeViewModel(category.Id, category.Name, category.Level, category.IsFamous, category.IsInternet, category.ParentId, category.IncludeInSearch, repository, categoryEditDialogService, self => RemoveFromParent(self, nodeDict), VisibleCategories, () => LoadAsync(CancellationToken.None), ancestorPath);
                nodeDict[category.Id] = node;
            }

            suppressVisibleCategoriesRebuild = true;
            try
            {
                Categories.Clear();

                foreach (var category in all.OrderBy(c => c.Level).ThenBy(c => c.Name))
                {
                    var node = nodeDict[category.Id];

                    if (category.ParentId is Option<FileClassificationCategoryId>.Some someParent && nodeDict.TryGetValue(someParent.Value, out var parentNode))
                        parentNode.Children.Add(node);
                    else
                        Categories.Add(node);
                }
            }
            finally
            {
                suppressVisibleCategoriesRebuild = false;
            }

            RebuildVisibleCategories();

            IsLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Exports the current taxonomy to a user-selected JSON file.</summary>
    public async Task ExportAsync(IStorageProvider storageProvider, CancellationToken cancellationToken = default)
    {
        string? path = await filePickerService.PickSaveFileAsync(storageProvider, "Export classifications", "classifications.json", "json", cancellationToken).ConfigureAwait(false);
        if (path is null)
            return;

        await exportImportService.ExportAsync(fileSystem.FileInfo.New(path), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Imports a taxonomy from a user-selected JSON file, replacing all existing data after confirmation.</summary>
    public async Task ImportAsync(IStorageProvider storageProvider, CancellationToken cancellationToken = default)
    {
        string? path = await filePickerService.PickOpenFileAsync(storageProvider, "Import classifications", "json", cancellationToken).ConfigureAwait(false);
        if (path is null)
            return;

        if (!await confirmationDialogService.ConfirmAsync("Import classifications", "This will update ALL existing classifications and add new ones. Continue?", cancellationToken).ConfigureAwait(false))
            return;

        await exportImportService.ImportAsync(fileSystem.FileInfo.New(path), cancellationToken).ConfigureAwait(false);
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    private bool CanAddCategory => !string.IsNullOrWhiteSpace(NewCategoryName);

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private async Task AddCategoryAsync()
    {
        var placeholder = new FileClassificationCategoryId(0);
        string trimmedName = NewCategoryName.Trim();

        await FileClassificationCategoryFactory.Create(placeholder, trimmedName, 1, IsFamous, IsInternet, Option.None<FileClassificationCategoryId>(), IncludeInSearch)
            .Match(category => AddValidatedCategoryAsync(category, trimmedName), _ => Task.CompletedTask)
            .ConfigureAwait(false);
    }

    private async Task AddValidatedCategoryAsync(FileClassificationCategory category, string trimmedName)
    {
        CategoryNodeViewModel? newNode = null;

        await repository.AddCategoryAsync(category, CancellationToken.None)
            .Tap(newId =>
            {
                newNode = new CategoryNodeViewModel(newId, trimmedName, 1, IsFamous, IsInternet, Option.None<FileClassificationCategoryId>(), IncludeInSearch, repository, categoryEditDialogService, self => Categories.Remove(self), VisibleCategories, () => LoadAsync(CancellationToken.None));
                Categories.Add(newNode);
            })
            .ConfigureAwait(false);

        NewCategoryName = string.Empty;
    }

    private void RemoveFromParent(CategoryNodeViewModel node, Dictionary<FileClassificationCategoryId, CategoryNodeViewModel> nodeDict)
    {
        if (node.Level == 1)
        {
            Categories.Remove(node);
            return;
        }

        foreach (var potentialParent in nodeDict.Values)
            potentialParent.Children.Remove(node);
    }

    private static string HasParentPath(CategoryNodeViewModel parentNode) => parentNode.HasAncestorPath ? $"{parentNode.AncestorPath} > {parentNode.Name}" : parentNode.Name;
}
