using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

public sealed partial class FileClassificationRuleRowViewModel : ObservableObject
{
    private readonly Func<int, Task> onDelete;

    public FileClassificationRuleRowViewModel(int id, FileClassificationRule rule, Func<int, Task> onDelete)
    {
        Id = id;
        Keywords = string.Join(", ", rule.Keywords);
        Level1 = rule.Classification.Level1;
        Level2 = rule.Classification.Level2.MapOrDefault(v => v, string.Empty);
        Level3 = rule.Classification.Level3.MapOrDefault(v => v, string.Empty);
        IsSpecial = rule.Classification.IsSpecial;
        this.onDelete = onDelete;
    }

    /// <summary>Database Id — used to identify the row for deletion.</summary>
    public int Id { get; }

    /// <inheritdoc />
    public string Keywords { get; }

    /// <inheritdoc />
    public string Level1 { get; }

    /// <inheritdoc />
    public string Level2 { get; }

    /// <inheritdoc />
    public string Level3 { get; }

    /// <inheritdoc />
    public bool IsSpecial { get; }

    [RelayCommand]
    private Task DeleteAsync() => onDelete(Id);
}
