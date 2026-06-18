using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Represents a single keyword row in the classification tree UI.</summary>
public sealed partial class KeywordRowViewModel : ObservableObject
{
    private readonly IFileClassificationRepository repository;
    private readonly Action<KeywordRowViewModel> onDeleteSelf;

    private string originalValue;
    private bool originalIsFamous;

    public KeywordRowViewModel(int keywordId, FileClassificationKeyword keyword, IFileClassificationRepository repository, Action<KeywordRowViewModel> onDeleteSelf)
    {
        KeywordId = keywordId;
        this.repository = repository;
        this.onDeleteSelf = onDeleteSelf;

        Value = keyword.Value;
        IsFamous = keyword.IsFamous.MapOrDefault(v => v, false);
        IsInternet = keyword.IsInternet.MapOrDefault(v => v, false);
        originalValue = Value;
        originalIsFamous = IsFamous;
    }

    /// <summary>Database identifier for this keyword.</summary>
    public int KeywordId { get; }

    [ObservableProperty]
    public partial string Value { get; set; }

    [ObservableProperty]
    public partial bool IsFamous { get; set; }

    [ObservableProperty]
    public partial bool IsInternet { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [RelayCommand]
    private void Edit() => IsEditing = true;

    [RelayCommand]
    private async Task SaveAsync() => await FileClassificationKeywordFactory.Create(Value, IsFamous ? Option.Some(true) : Option.None<bool>(), IsInternet ? Option.Some(true) : Option.None<bool>())
            .Match(PersistKeywordAsync, _ => Task.CompletedTask)
            .ConfigureAwait(false);

    private async Task PersistKeywordAsync(FileClassificationKeyword keyword)
    {
        await repository.UpdateKeywordAsync(KeywordId, keyword, CancellationToken.None).ConfigureAwait(false);
        originalValue = Value;
        originalIsFamous = IsFamous;
        IsEditing = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        Value = originalValue;
        IsFamous = originalIsFamous;
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        await repository.DeleteKeywordAsync(KeywordId, CancellationToken.None).ConfigureAwait(false);
        onDeleteSelf(this);
    }
}
