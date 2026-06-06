using System.Collections.ObjectModel;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Represents a category node in the file classification hierarchy tree.</summary>
public sealed partial class CategoryNodeViewModel : ObservableObject
{
    private readonly IFileClassificationRepository repository;
    private readonly Action<CategoryNodeViewModel> onDeleteSelf;

    public CategoryNodeViewModel(FileClassificationCategoryId categoryId, string name, int level, IFileClassificationRepository repository, Action<CategoryNodeViewModel> onDeleteSelf)
    {
        CategoryId = categoryId;
        Name = name;
        Level = level;
        this.repository = repository;
        this.onDeleteSelf = onDeleteSelf;

        Children.CollectionChanged += (_, _) => AddKeywordCommand.NotifyCanExecuteChanged();
    }

    /// <summary>The database identifier for this category.</summary>
    public FileClassificationCategoryId CategoryId { get; }

    /// <summary>Display name of this category.</summary>
    public string Name { get; }

    /// <summary>Hierarchy level (1 = root, 2 = child, 3 = grandchild).</summary>
    public int Level { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>Child categories nested under this node.</summary>
    public ObservableCollection<CategoryNodeViewModel> Children { get; } = [];

    /// <summary>Keywords assigned to this category (only populated for leaf nodes).</summary>
    public ObservableCollection<KeywordRowViewModel> Keywords { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddKeywordCommand))]
    public partial string NewKeyword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewKeywordIsSpecial { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddChildCategoryCommand))]
    public partial string NewChildCategoryName { get; set; } = string.Empty;

    private bool CanAddKeyword => !string.IsNullOrWhiteSpace(NewKeyword) && Children.Count == 0;

    [RelayCommand(CanExecute = nameof(CanAddKeyword))]
    private async Task AddKeywordAsync()
    {
        var keywordResult = FileClassificationKeywordFactory.Create(NewKeyword.Trim(), NewKeywordIsSpecial ? Option.Some(true) : Option.None<bool>());
        if (keywordResult is not Result<FileClassificationKeyword, string>.Ok okKeyword)
            return;

        await repository.AddKeywordAsync(CategoryId, okKeyword.Value, CancellationToken.None)
            .TapAsync(keywordId => Keywords.Add(new KeywordRowViewModel(keywordId, okKeyword.Value, repository, self => Keywords.Remove(self))))
            .ConfigureAwait(false);

        NewKeyword = string.Empty;
        NewKeywordIsSpecial = false;
    }

    private bool CanAddChildCategory => !string.IsNullOrWhiteSpace(NewChildCategoryName) && Level < 3;

    [RelayCommand(CanExecute = nameof(CanAddChildCategory))]
    private async Task AddChildCategoryAsync()
    {
        int childLevel = Level + 1;
        var placeholder = new FileClassificationCategoryId(0);
        string trimmedName = NewChildCategoryName.Trim();

        var categoryResult = FileClassificationCategoryFactory.Create(placeholder, trimmedName, childLevel, Option.Some(CategoryId));
        if (categoryResult is not Result<FileClassificationCategory, string>.Ok okCategory)
            return;

        await repository.AddCategoryAsync(okCategory.Value, CancellationToken.None)
            .TapAsync(newId => Children.Add(new CategoryNodeViewModel(newId, trimmedName, childLevel, repository, self => Children.Remove(self))))
            .ConfigureAwait(false);

        NewChildCategoryName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteSelfAsync()
    {
        await repository.DeleteCategoryAsync(CategoryId, CancellationToken.None).ConfigureAwait(false);
        onDeleteSelf(this);
    }
}
