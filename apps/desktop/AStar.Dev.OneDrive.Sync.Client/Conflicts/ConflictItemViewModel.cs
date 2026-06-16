using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

public sealed partial class ConflictItemViewModel : ObservableObject
{
    private readonly SyncConflict conflict;
    private readonly ISyncService syncService;
    private readonly ILocalizationService loc;

    public ConflictItemViewModel(SyncConflict conflict, ISyncService syncService, ILocalizationService loc)
    {
        this.conflict = conflict;
        this.syncService = syncService;
        this.loc = loc;
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, SelectedPolicy);
        loc.CultureChanged += OnCultureChanged;
    }

    public Guid Id => conflict.Id;
    public string AccountId => conflict.Remote.AccountId.Id;
    public string FileName => Path.GetFileName(conflict.Target.RelativePath);
    public string RelativePath => conflict.Target.RelativePath;
    public DateTimeOffset LocalModified => conflict.Snapshot.LocalModified;
    public DateTimeOffset RemoteModified => conflict.Snapshot.RemoteModified;
    public long LocalSize => conflict.Snapshot.LocalSize;
    public long RemoteSize => conflict.Snapshot.RemoteSize;
    public DateTimeOffset DetectedAt => conflict.DetectedAt;

    public string LocalModifiedText => FormatDateTime(conflict.Snapshot.LocalModified);
    public string RemoteModifiedText => FormatDateTime(conflict.Snapshot.RemoteModified);
    public string LocalSizeText => conflict.Snapshot.LocalSize.FileSizeToText();
    public string RemoteSizeText => conflict.Snapshot.RemoteSize.FileSizeToText();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelOpen))]
    [NotifyPropertyChangedFor(nameof(CollapseExpandLabel))]
    public partial bool IsExpanded { get; set; }

    public bool IsPanelOpen => IsExpanded;

    /// <summary>Label for the expand/collapse toggle button, localised for the current culture.</summary>
    public string CollapseExpandLabel => loc.GetLocal(IsExpanded ? "Conflict.Collapse" : "Conflict.Resolve");

    /// <summary>Localised "resolved" badge label.</summary>
    public string ResolvedText => loc.GetLocal("Activity.Conflict.Resolved");

    [ObservableProperty]
    public partial bool IsResolving { get; set; }

    [ObservableProperty]
    public partial bool IsResolved { get; set; }

    [ObservableProperty]
    public partial ConflictPolicy SelectedPolicy { get; set; } = ConflictPolicy.Ignore;

    partial void OnSelectedPolicyChanged(ConflictPolicy value)
    {
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, value);
        OnPropertyChanged(nameof(PolicyOptions));
    }

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; private set; }

    public event EventHandler<ConflictItemViewModel>? Resolved;

    [RelayCommand]
    private void TogglePanel() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private async Task ResolveAsync()
    {
        if(IsResolving)
            return;

        IsResolving = true;
        try
        {
            await syncService.ResolveConflictAsync(conflict, SelectedPolicy);
            IsResolved = true;
            IsExpanded = false;
            Resolved?.Invoke(this, this);
        }
        finally
        {
            IsResolving = false;
        }
    }

    [RelayCommand]
    private void Dismiss()
    {
        IsExpanded = false;
        IsResolved = true;
        Resolved?.Invoke(this, this);
    }

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, SelectedPolicy);
        OnPropertyChanged(nameof(PolicyOptions));
        OnPropertyChanged(nameof(CollapseExpandLabel));
        OnPropertyChanged(nameof(ResolvedText));
    }

    private static string FormatDateTime(DateTimeOffset dt)
        => dt.LocalDateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture);
}
