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
    private bool originalIsSpecialOverride;

    public KeywordRowViewModel(int keywordId, FileClassificationKeyword keyword, IFileClassificationRepository repository, Action<KeywordRowViewModel> onDeleteSelf)
    {
        KeywordId = keywordId;
        this.repository = repository;
        this.onDeleteSelf = onDeleteSelf;

        Value = keyword.Value;
        IsSpecialOverride = keyword.IsSpecialOverride.MapOrDefault(v => v, false);

        originalValue = Value;
        originalIsSpecialOverride = IsSpecialOverride;
    }

    /// <summary>Database identifier for this keyword.</summary>
    public int KeywordId { get; }

    [ObservableProperty]
    public partial string Value { get; set; }

    [ObservableProperty]
    public partial bool IsSpecialOverride { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [RelayCommand]
    private void Edit() => IsEditing = true;

    [RelayCommand]
    private async Task SaveAsync()
    {
        var keywordResult = FileClassificationKeywordFactory.Create(Value, IsSpecialOverride ? Option.Some(true) : Option.None<bool>());
        if (keywordResult is not Result<FileClassificationKeyword, string>.Ok okResult)
            return;

        await repository.UpdateKeywordAsync(KeywordId, okResult.Value, CancellationToken.None).ConfigureAwait(false);

        originalValue = Value;
        originalIsSpecialOverride = IsSpecialOverride;
        IsEditing = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        Value = originalValue;
        IsSpecialOverride = originalIsSpecialOverride;
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        await repository.DeleteKeywordAsync(KeywordId, CancellationToken.None).ConfigureAwait(false);
        onDeleteSelf(this);
    }
}
