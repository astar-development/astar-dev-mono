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
    private readonly ILocalizationService localizationService;
    private readonly IFileSystem fileSystem;

    public FileClassificationRulesViewModel(IFileClassificationRepository repository, IFileClassificationExportImportService exportImportService, IFilePickerService filePickerService, IConfirmationDialogService confirmationDialogService, ILocalizationService localizationService, IFileSystem fileSystem)
    {
        this.repository = repository;
        this.exportImportService = exportImportService;
        this.filePickerService = filePickerService;
        this.confirmationDialogService = confirmationDialogService;
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
        VisibleCategories.Add(node);
        foreach (var child in node.Children)
            AppendVisible(child);
    }

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
                var node = new CategoryNodeViewModel(category.Id, category.Name, category.Level, category.IsFamous, category.IsInternet, category.ParentId, repository, self => RemoveFromParent(self, nodeDict));
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
                newNode = new CategoryNodeViewModel(newId, trimmedName, 1, IsFamous, IsInternet, Option.None<FileClassificationCategoryId>(), repository, self => Categories.Remove(self));
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
}
