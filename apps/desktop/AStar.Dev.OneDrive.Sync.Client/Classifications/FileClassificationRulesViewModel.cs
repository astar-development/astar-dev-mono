using System.Collections.ObjectModel;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>View model for the file classification rules tree view.</summary>
public sealed partial class FileClassificationRulesViewModel : ObservableObject
{
    private readonly IFileClassificationRepository repository;

    public FileClassificationRulesViewModel(IFileClassificationRepository repository)
    {
        this.repository = repository;
        Categories.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoCategories));
    }

    /// <summary>Root-level category nodes.</summary>
    public ObservableCollection<CategoryNodeViewModel> Categories { get; } = [];

    /// <summary>True when no categories have been loaded.</summary>
    public bool HasNoCategories => Categories.Count == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
    public partial string NewCategoryName { get; set; } = string.Empty;

    /// <summary>Loads all categories from the repository and builds the tree, then populates keywords for each leaf node.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var all = await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);

        var nodeDict = new Dictionary<FileClassificationCategoryId, CategoryNodeViewModel>();

        foreach (var category in all.OrderBy(c => c.Level))
        {
            var node = new CategoryNodeViewModel(category.Id, category.Name, category.Level, repository, self => RemoveFromParent(self, nodeDict));
            nodeDict[category.Id] = node;
        }

        Categories.Clear();

        foreach (var category in all.OrderBy(c => c.Level))
        {
            var node = nodeDict[category.Id];

            if (category.ParentId is Option<FileClassificationCategoryId>.Some someParent && nodeDict.TryGetValue(someParent.Value, out var parentNode))
                parentNode.Children.Add(node);
            else
                Categories.Add(node);
        }

        var leafNodes = nodeDict.Values.Where(node => node.Children.Count == 0).ToList();

        foreach (var leafNode in leafNodes)
        {
            var keywords = await repository.GetKeywordsForCategoryAsync(leafNode.CategoryId, cancellationToken).ConfigureAwait(false);

            foreach (var entry in keywords)
                leafNode.Keywords.Add(new KeywordRowViewModel(entry.Id, entry.Keyword, repository, self => leafNode.Keywords.Remove(self)));
        }
    }

    private bool CanAddCategory => !string.IsNullOrWhiteSpace(NewCategoryName);

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private async Task AddCategoryAsync()
    {
        var placeholder = new FileClassificationCategoryId(0);
        string trimmedName = NewCategoryName.Trim();
        var categoryResult = FileClassificationCategoryFactory.Create(placeholder, trimmedName, 1, Option.None<FileClassificationCategoryId>());

        if (categoryResult is not Result<FileClassificationCategory, string>.Ok okCategory)
            return;

        await repository.AddCategoryAsync(okCategory.Value, CancellationToken.None)
            .TapAsync(newId => Categories.Add(new CategoryNodeViewModel(newId, trimmedName, 1, repository, self => Categories.Remove(self))))
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
