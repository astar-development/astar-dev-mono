using System.Collections.ObjectModel;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Represents a category node in the file classification hierarchy tree.</summary>
public sealed partial class CategoryNodeViewModel : ObservableObject
{
    private readonly IFileClassificationRepository repository;
    private readonly Action<CategoryNodeViewModel> onDeleteSelf;

    public CategoryNodeViewModel(FileClassificationCategoryId categoryId, string name, int level, bool isFamous, bool isInternet, IFileClassificationRepository repository, Action<CategoryNodeViewModel> onDeleteSelf)
    {
        CategoryId = categoryId;
        Name = name;
        Level = level;
        IsFamous = isFamous;
        IsInternet = isInternet;
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
    [NotifyCanExecuteChangedFor(nameof(AddKeywordCommand))]
    public partial bool IsFamous { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddKeywordCommand))]
    public partial bool IsInternet { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddChildCategoryCommand))]
    public partial string NewChildCategoryName { get; set; } = string.Empty;

    private bool CanAddKeyword => !string.IsNullOrWhiteSpace(NewKeyword) && Children.Count == 0;

    [RelayCommand(CanExecute = nameof(CanAddKeyword))]
    private async Task AddKeywordAsync() => await FileClassificationKeywordFactory.Create(NewKeyword.Trim(), IsFamous ? Option.Some(true) : Option.None<bool>(), IsInternet ? Option.Some(true) : Option.None<bool>())
            .Match(AddValidatedKeywordAsync, _ => Task.CompletedTask)
            .ConfigureAwait(false);

    private async Task AddValidatedKeywordAsync(FileClassificationKeyword keyword)
    {
        await repository.AddKeywordAsync(CategoryId, keyword, CancellationToken.None)
            .TapAsync(keywordId => Keywords.Add(new KeywordRowViewModel(keywordId, keyword, repository, self => Keywords.Remove(self))))
            .ConfigureAwait(false);

        NewKeyword = string.Empty;
        IsFamous = false;
        IsInternet = false;
    }

    private bool CanAddChildCategory => !string.IsNullOrWhiteSpace(NewChildCategoryName) && Level < 3;

    [RelayCommand(CanExecute = nameof(CanAddChildCategory))]
    private async Task AddChildCategoryAsync()
    {
        int childLevel = Level + 1;
        var placeholder = new FileClassificationCategoryId(0);
        string trimmedName = NewChildCategoryName.Trim().ToTitleCase();

        await FileClassificationCategoryFactory.Create(placeholder, trimmedName, childLevel, IsFamous, IsInternet, Option.Some(CategoryId))
            .Match(category => AddValidatedChildCategoryAsync(category, trimmedName, childLevel), _ => Task.CompletedTask)
            .ConfigureAwait(false);
    }

    private async Task AddValidatedChildCategoryAsync(FileClassificationCategory category, string trimmedName, int childLevel)
    {
        CategoryNodeViewModel? newChild = null;

        await repository.AddCategoryAsync(category, CancellationToken.None)
            .TapAsync(newId =>
            {
                newChild = new CategoryNodeViewModel(newId, trimmedName, childLevel, IsFamous, IsInternet, repository, self => Children.Remove(self));
                Children.Add(newChild);
            })
            .TapAsync(async _ =>
            {
                if (newChild is null)
                    return;

                await FileClassificationKeywordFactory.Create(trimmedName, IsFamous ? Option.Some(true) : Option.None<bool>(), IsInternet ? Option.Some(true) : Option.None<bool>())
                    .Match(keyword => AddKeywordToChildAsync(newChild, keyword), _ => Task.CompletedTask)
                    .ConfigureAwait(false);
            })
            .ConfigureAwait(false);

        NewChildCategoryName = string.Empty;
    }

    private async Task AddKeywordToChildAsync(CategoryNodeViewModel child, FileClassificationKeyword keyword) => await repository.AddKeywordAsync(child.CategoryId, keyword, CancellationToken.None)
            .TapAsync(keywordId => child.Keywords.Add(new KeywordRowViewModel(keywordId, keyword, repository, self => child.Keywords.Remove(self))))
            .ConfigureAwait(false);

    [RelayCommand]
    private async Task DeleteSelfAsync()
    {
        await repository.DeleteCategoryAsync(CategoryId, CancellationToken.None).ConfigureAwait(false);
        onDeleteSelf(this);
    }
}
