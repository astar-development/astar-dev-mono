using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

public sealed partial class FileClassificationRuleRowViewModel : ObservableObject
{
    private readonly Func<int, Task> onDelete;
    private readonly Func<int, FileClassificationRule, Task> onUpdate;

    private string originalKeywords;
    private string originalLevel1;
    private string originalLevel2;
    private string originalLevel3;
    private bool originalIsSpecial;

    public FileClassificationRuleRowViewModel(int id, FileClassificationRule rule, Func<int, Task> onDelete, Func<int, FileClassificationRule, Task> onUpdate)
    {
        Id = id;
        this.onDelete = onDelete;
        this.onUpdate = onUpdate;

        Keywords = string.Join(", ", rule.Keywords);
        Level1 = rule.Classification.Level1;
        Level2 = rule.Classification.Level2.MapOrDefault(v => v, string.Empty);
        Level3 = rule.Classification.Level3.MapOrDefault(v => v, string.Empty);
        IsSpecial = rule.Classification.IsSpecial;

        originalKeywords = Keywords;
        originalLevel1 = Level1;
        originalLevel2 = Level2;
        originalLevel3 = Level3;
        originalIsSpecial = IsSpecial;
    }

    /// <summary>Database Id — used to identify the row for deletion.</summary>
    public int Id { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Keywords { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Level1 { get; set; }

    [ObservableProperty]
    public partial string Level2 { get; set; }

    [ObservableProperty]
    public partial string Level3 { get; set; }

    [ObservableProperty]
    public partial bool IsSpecial { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    private bool CanSave => !string.IsNullOrWhiteSpace(Keywords) && !string.IsNullOrWhiteSpace(Level1);

    [RelayCommand]
    private Task EditAsync()
    {
        IsEditing = true;
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var keywords = Keywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList()
            .AsReadOnly();

        var level2 = string.IsNullOrWhiteSpace(Level2) ? Option.None<string>() : Option.Some(Level2.Trim());
        var level3 = string.IsNullOrWhiteSpace(Level3) ? Option.None<string>() : Option.Some(Level3.Trim());
        var rule = FileClassificationRuleFactory.Create(
            keywords,
            FileClassificationFactory.Create(Level1.Trim(), level2, level3, IsSpecial));

        await onUpdate(Id, rule);

        originalKeywords = Keywords;
        originalLevel1 = Level1;
        originalLevel2 = Level2;
        originalLevel3 = Level3;
        originalIsSpecial = IsSpecial;
        IsEditing = false;
    }

    [RelayCommand]
    private Task CancelAsync()
    {
        Keywords = originalKeywords;
        Level1 = originalLevel1;
        Level2 = originalLevel2;
        Level3 = originalLevel3;
        IsSpecial = originalIsSpecial;
        IsEditing = false;

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DeleteAsync() => onDelete(Id);
}
