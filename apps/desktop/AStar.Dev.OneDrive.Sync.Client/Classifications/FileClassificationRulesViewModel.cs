using System.Collections.ObjectModel;
using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
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
        Categories.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoCategories));
    }

    /// <summary>Root-level category nodes.</summary>
    public ObservableCollection<CategoryNodeViewModel> Categories { get; } = [];

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

            Categories.Clear();

            foreach (var category in all.OrderBy(c => c.Level).ThenBy(c => c.Name))
            {
                var node = nodeDict[category.Id];

                if (category.ParentId is Option<FileClassificationCategoryId>.Some someParent && nodeDict.TryGetValue(someParent.Value, out var parentNode))
                    parentNode.Children.Add(node);
                else
                    Categories.Add(node);
            }

            // var leafNodes = nodeDict.Values.Where(node => node.Children.Count == 0).ToList();

            // foreach (var leafNode in leafNodes)
            // {
            //     var keywords = await repository.GetKeywordsForCategoryAsync(leafNode.CategoryId, cancellationToken).ConfigureAwait(false);

            //     foreach (var entry in keywords)
            //         leafNode.Keywords.Add(new KeywordRowViewModel(entry.Id, entry.Keyword, repository, self => leafNode.Keywords.Remove(self)));
            // }

            IsLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Exports the current taxonomy to a user-selected JSON file.</summary>
    public async Task ExportAsync(IStorageProvider storageProvider, CancellationToken ct = default)
    {
        string? path = await filePickerService.PickSaveFileAsync(storageProvider, "Export classifications", "classifications.json", "json", ct).ConfigureAwait(false);
        if (path is null)
            return;

        await exportImportService.ExportAsync(fileSystem.FileInfo.New(path), ct).ConfigureAwait(false);
    }

    /// <summary>Imports a taxonomy from a user-selected JSON file, replacing all existing data after confirmation.</summary>
    public async Task ImportAsync(IStorageProvider storageProvider, CancellationToken ct = default)
    {
        string? path = await filePickerService.PickOpenFileAsync(storageProvider, "Import classifications", "json", ct).ConfigureAwait(false);
        if (path is null)
            return;

        bool confirmed = await confirmationDialogService.ConfirmAsync("Import classifications", "This will delete ALL classifications. Continue?", ct).ConfigureAwait(false);
        if (!confirmed)
            return;

        await exportImportService.ImportAsync(fileSystem.FileInfo.New(path), ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
    }

    private bool CanAddCategory => !string.IsNullOrWhiteSpace(NewCategoryName);

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private async Task AddCategoryAsync()
    {
        var placeholder = new FileClassificationCategoryId(0);
        string trimmedName = NewCategoryName.Trim();

        await FileClassificationCategoryFactory.Create(placeholder, trimmedName, 1, IsFamous, IsInternet, Option.None<FileClassificationCategoryId>())
            .Match(category => AddValidatedCategoryAsync(category, trimmedName), _ => Task.CompletedTask)
            .ConfigureAwait(false);
    }

    private async Task AddValidatedCategoryAsync(FileClassificationCategory category, string trimmedName)
    {
        CategoryNodeViewModel? newNode = null;

        await repository.AddCategoryAsync(category, CancellationToken.None)
            .TapAsync(newId =>
            {
                newNode = new CategoryNodeViewModel(newId, trimmedName, 1, IsFamous, IsInternet, Option.None<FileClassificationCategoryId>(), repository, self => Categories.Remove(self));
                Categories.Add(newNode);
            })
            .TapAsync(async _ =>
            {
                if (newNode is null)
                    return;

                // await FileClassificationKeywordFactory.Create(trimmedName, IsFamous ? Option.Some(true) : Option.None<bool>(), IsInternet ? Option.Some(true) : Option.None<bool>())
                //     .Match(keyword => AddKeywordToNewNodeAsync(newNode, keyword), _ => Task.CompletedTask)
                //     .ConfigureAwait(false);
            })
            .ConfigureAwait(false);

        NewCategoryName = string.Empty;
    }

    // private async Task AddKeywordToNewNodeAsync(CategoryNodeViewModel node, FileClassificationKeyword keyword) => await repository.AddKeywordAsync(node.CategoryId, keyword, CancellationToken.None)
    //         .TapAsync(keywordId => node.Keywords.Add(new KeywordRowViewModel(keywordId, keyword, repository, self => node.Keywords.Remove(self))))
    //         .ConfigureAwait(false);

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
