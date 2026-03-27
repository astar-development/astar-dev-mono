using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.old.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class ActivityViewModel : ReactiveObject
{
    private readonly IConflictResolver _resolver;

    public ActivityViewModel() : this(new ConflictResolver(new JsonConflictStore(GetDefaultStorePath())))
    {
    }

    public ActivityViewModel(IConflictResolver resolver)
    {
        _resolver = resolver;

        SwitchTabCommand = ReactiveCommand.Create<ActivityTab>(tab =>
        {
            IsLogTabActive       = tab == ActivityTab.Log;
            IsConflictsTabActive = tab == ActivityTab.Conflicts;
        });
        SetFilterCommand    = ReactiveCommand.Create<ActivityItemType?>(filter => { });
        ClearLogCommand     = ReactiveCommand.Create(() => FilteredLog.Clear());
        SelectAllCommand    = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand  = ReactiveCommand.Create(DeselectAll);
        ApplyToSelectedCommand = ReactiveCommand.CreateFromTask(ApplyToSelectedAsync);

        Conflicts.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(HasConflicts));
            this.RaisePropertyChanged(nameof(ConflictBadgeText));
        };
    }

    public bool IsLogTabActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool IsConflictsTabActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool HasLogItems => FilteredLog.Count > 0;
    public bool HasConflicts => Conflicts.Count > 0;
    public string ConflictBadgeText => Conflicts.Count.ToString(CultureInfo.InvariantCulture);

    public bool HasAnySelected
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ConflictPolicy? BulkPolicy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ObservableCollection<ActivityItemViewModel> FilteredLog { get; } = [];
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];

    public ObservableCollection<ConflictPolicyOption> BulkPolicyOptions { get; } =
    [
        new() { Policy = ConflictPolicy.LocalWins,  Label = "Local wins",  Description = "Keep the local file, discard the remote version." },
        new() { Policy = ConflictPolicy.RemoteWins, Label = "Remote wins", Description = "Keep the remote file, discard the local version." },
        new() { Policy = ConflictPolicy.KeepBoth,   Label = "Keep both",   Description = "Rename the conflicting copy and keep both files." },
        new() { Policy = ConflictPolicy.Skip,       Label = "Skip",        Description = "Defer this conflict — it will be queued for later." }
    ];

    public ICommand SwitchTabCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand ApplyToSelectedCommand { get; }

    public async Task LoadConflictsAsync()
    {
        var pending = await _resolver.GetPendingAsync().ConfigureAwait(true);
        Conflicts.Clear();
        foreach (var record in pending)
        {
            AddConflictViewModel(record);
        }
    }

    public void NotifySelectionChanged()
    {
        HasAnySelected = Conflicts.Any(c => c.IsSelected && !c.IsResolved);
    }

    private void SelectAll()
    {
        foreach (var conflict in Conflicts)
        {
            if (!conflict.IsResolved)
            {
                conflict.IsSelected = true;
            }
        }

        NotifySelectionChanged();
    }

    private void DeselectAll()
    {
        foreach (var conflict in Conflicts)
        {
            conflict.IsSelected = false;
        }

        NotifySelectionChanged();
    }

    private async Task ApplyToSelectedAsync()
    {
        if (BulkPolicy is not { } policy)
        {
            return;
        }

        var selectedIds = Conflicts
            .Where(c => c.IsSelected && !c.IsResolved)
            .Select(c => c.ConflictId)
            .ToList();

        if (selectedIds.Count == 0)
        {
            return;
        }

        var resolvedIds = await _resolver.ResolveAsync(selectedIds, policy).ConfigureAwait(true);

        foreach (var conflict in Conflicts)
        {
            if (resolvedIds.Contains(conflict.ConflictId))
            {
                conflict.IsResolved = true;
                conflict.IsResolving = false;
                conflict.IsPanelOpen = false;
                conflict.IsSelected = false;
                conflict.SelectedPolicy = policy;
            }
        }

        BulkPolicy = null;
        NotifySelectionChanged();
    }

    private ConflictItemViewModel AddConflictViewModel(ConflictRecord record)
    {
        var vm = new ConflictItemViewModel
        {
            ConflictId = record.Id,
            FileName = record.FileName,
            RelativePath = record.FilePath,
            LocalModifiedText = record.LocalModifiedUtc?.ToString("g", CultureInfo.CurrentCulture) ?? "Deleted",
            LocalSizeText = record.LocalSizeBytes is { } lb ? FormatBytes(lb) : "—",
            RemoteModifiedText = record.RemoteModifiedUtc?.ToString("g", CultureInfo.CurrentCulture) ?? "Deleted",
            RemoteSizeText = record.RemoteSizeBytes is { } rb ? FormatBytes(rb) : "—"
        };

        vm.TogglePanelCommand = ReactiveCommand.Create(() =>
        {
            vm.IsExpanded = !vm.IsExpanded;
            vm.IsPanelOpen = vm.IsExpanded;
        });

        vm.DismissCommand = ReactiveCommand.Create(() =>
        {
            vm.IsPanelOpen = false;
            vm.IsExpanded = false;
            vm.SelectedPolicy = null;
        });

        vm.ResolveCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (vm.SelectedPolicy is not { } policy)
            {
                return;
            }

            vm.IsResolving = true;
            var resolvedIds = await _resolver.ResolveAsync([vm.ConflictId], policy).ConfigureAwait(true);

            foreach (var conflict in Conflicts)
            {
                if (resolvedIds.Contains(conflict.ConflictId))
                {
                    conflict.IsResolved = true;
                    conflict.IsResolving = false;
                    conflict.IsPanelOpen = false;
                    conflict.SelectedPolicy = policy;
                }
            }
        });

        Conflicts.Add(vm);
        return vm;
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
        };
    }

    private static string GetDefaultStorePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "AStar.Dev.OneDriveSync.old", "conflicts.json");
    }
}
