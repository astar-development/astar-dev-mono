using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class ConflictItemViewModel : ReactiveObject
{
    private bool _isExpanded;
    private bool _isPanelOpen;
    private bool _isResolved;
    private bool _isResolving;
    private ConflictPolicy? _selectedPolicy;

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

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public bool IsPanelOpen
    {
        get => _isPanelOpen;
        set => this.RaiseAndSetIfChanged(ref _isPanelOpen, value);
    }

    public bool IsResolved
    {
        get => _isResolved;
        set => this.RaiseAndSetIfChanged(ref _isResolved, value);
    }

    public bool IsResolving
    {
        get => _isResolving;
        set => this.RaiseAndSetIfChanged(ref _isResolving, value);
    }

    public ConflictPolicy? SelectedPolicy
    {
        get => _selectedPolicy;
        set => this.RaiseAndSetIfChanged(ref _selectedPolicy, value);
    }

    public ICommand TogglePanelCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ResolveCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand DismissCommand { get; init; } = ReactiveCommand.Create(() => { });
}
