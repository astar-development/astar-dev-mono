using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

public sealed partial class FileClassificationRulesViewModel : ObservableObject
{
    private readonly IFileClassificationRuleRepository repository;

    public FileClassificationRulesViewModel(IFileClassificationRuleRepository repository)
    {
        this.repository = repository;
        Rules.CollectionChanged += OnRulesChanged;
    }

    public ObservableCollection<FileClassificationRuleRowViewModel> Rules { get; } = [];

    public bool HasNoRules => Rules.Count == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewKeywords { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewLevel1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewLevel2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewLevel3 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewIsSpecial { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var entries = await repository.GetAllWithIdsAsync(cancellationToken);
        Rules.Clear();
        foreach (var entry in entries)
        {
            Rules.Add(new FileClassificationRuleRowViewModel(entry.Id, entry.Rule, DeleteRuleAsync, UpdateRuleAsync));
        }
    }

    private bool CanAdd => !string.IsNullOrWhiteSpace(NewKeywords) && !string.IsNullOrWhiteSpace(NewLevel1);

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var keywords = NewKeywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList()
            .AsReadOnly();
        var level2 = string.IsNullOrWhiteSpace(NewLevel2) ? Option.None<string>() : Option.Some(NewLevel2.Trim());
        var level3 = string.IsNullOrWhiteSpace(NewLevel3) ? Option.None<string>() : Option.Some(NewLevel3.Trim());
        var classification = FileClassificationFactory.Create(NewLevel1.Trim(), level2, level3, NewIsSpecial);
        var rule = FileClassificationRuleFactory.Create(keywords, classification);

        int id = await repository.AddAsync(rule);
        Rules.Add(new FileClassificationRuleRowViewModel(id, rule, DeleteRuleAsync, UpdateRuleAsync));

        NewKeywords = string.Empty;
        NewLevel1 = string.Empty;
        NewLevel2 = string.Empty;
        NewLevel3 = string.Empty;
        NewIsSpecial = false;
    }

    private async Task UpdateRuleAsync(int id, FileClassificationRule rule)
    {
        await repository.UpdateAsync(id, rule, CancellationToken.None);
    }

    private async Task DeleteRuleAsync(int id)
    {
        await repository.DeleteAsync(id);
        var row = Rules.FirstOrDefault(r => r.Id == id);
        if (row is not null)
            Rules.Remove(row);
    }

    private void OnRulesChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(HasNoRules));
}
