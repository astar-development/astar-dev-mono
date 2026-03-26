using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.Conflict.Resolution;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class ConflictItemViewModel : ReactiveObject
{
    public Guid ConflictId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string LocalModifiedText { get; init; } = string.Empty;
    public string LocalSizeText { get; init; } = string.Empty;
    public string RemoteModifiedText { get; init; } = string.Empty;
    public string RemoteSizeText { get; init; } = string.Empty;

    public ObservableCollection<ConflictPolicyOption> PolicyOptions { get; } =
    [
        new() { Policy = ConflictPolicy.LocalWins,  Label = "Local wins",  Description = "Keep the local file, discard the remote version." },
        new() { Policy = ConflictPolicy.RemoteWins, Label = "Remote wins", Description = "Keep the remote file, discard the local version." },
        new() { Policy = ConflictPolicy.KeepBoth,   Label = "Keep both",   Description = "Rename the conflicting copy and keep both files." },
        new() { Policy = ConflictPolicy.Skip,       Label = "Skip",        Description = "Defer this conflict — it will be queued for later." }
    ];

    public bool IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsExpanded
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsPanelOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsResolved
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsResolving
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ConflictPolicy? SelectedPolicy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand TogglePanelCommand { get; set; } = ReactiveCommand.Create(() => { });
    public ICommand ResolveCommand { get; set; } = ReactiveCommand.Create(() => { });
    public ICommand DismissCommand { get; set; } = ReactiveCommand.Create(() => { });
}
