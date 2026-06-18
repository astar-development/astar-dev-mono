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
    private readonly Option<FileClassificationCategoryId> parentId;
    private bool originalIsFamous;
    private bool originalIsInternet;

    public CategoryNodeViewModel(FileClassificationCategoryId categoryId, string name, int level, bool isFamous, bool isInternet, Option<FileClassificationCategoryId> parentId, IFileClassificationRepository repository, Action<CategoryNodeViewModel> onDeleteSelf)
    {
        CategoryId = categoryId;
        Name = name;
        Level = level;
        IsFamous = isFamous;
        IsInternet = isInternet;
        this.parentId = parentId;
        this.repository = repository;
        this.onDeleteSelf = onDeleteSelf;

        Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsLeafNode));
    }

    /// <summary>The database identifier for this category.</summary>
    public FileClassificationCategoryId CategoryId { get; }

    /// <summary>Display name of this category.</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>Hierarchy level (1 = root, 2 = child, 3 = grandchild).</summary>
    public int Level { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string EditedName { get; set; } = string.Empty;

    /// <summary>Child categories nested under this node.</summary>
    public ObservableCollection<CategoryNodeViewModel> Children { get; } = [];

    /// <summary>True when this node has no children and can therefore hold keywords.</summary>
    public bool IsLeafNode => Children.Count == 0;

    /// <summary>Keywords assigned to this category (only populated for leaf nodes).</summary>
    public ObservableCollection<KeywordRowViewModel> Keywords { get; } = [];

    [ObservableProperty]
    //[NotifyCanExecuteChangedFor(nameof(AddKeywordCommand))]
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
    private void Edit()
    {
        EditedName = Name;
        originalIsFamous = IsFamous;
        originalIsInternet = IsInternet;
        IsEditing = true;
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

        await FileClassificationCategoryFactory.Create(CategoryId, trimmedName, Level, IsFamous, IsInternet, parentId)
            .Match(category => PersistUpdateAsync(category, trimmedName), _ => Task.CompletedTask)
            .ConfigureAwait(false);
    }

    private async Task PersistUpdateAsync(FileClassificationCategory category, string trimmedName)
    {
        await repository.UpdateCategoryAsync(CategoryId, category, CancellationToken.None)
            .TapAsync(_ =>
            {
                Name = trimmedName;
                IsEditing = false;
            })
            .ConfigureAwait(false);
    }

    [RelayCommand]
    private void Cancel()
    {
        EditedName = string.Empty;
        IsFamous = originalIsFamous;
        IsInternet = originalIsInternet;
        IsEditing = false;
    }

    private bool CanAddKeyword => !string.IsNullOrWhiteSpace(NewKeyword) && Children.Count == 0;

    // TODO: Implement AddKeywordAsync when AddKeywordAsync is available in IFileClassificationRepository
    [RelayCommand(CanExecute = nameof(CanAddKeyword))]
    private Task AddKeywordAsync() => Task.CompletedTask;

    private bool CanAddCategory => !string.IsNullOrWhiteSpace(NewChildCategoryName) && Level < 3;

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
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
        await repository.AddCategoryAsync(category, CancellationToken.None)
            .TapAsync(newId =>
            {
                var newChild = new CategoryNodeViewModel(newId, trimmedName, childLevel, IsFamous, IsInternet, Option.Some(CategoryId), repository, self => Children.Remove(self));
                Children.Add(newChild);
            })
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
