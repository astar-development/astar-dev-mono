using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.OneDriveSyncClient.Conflicts;

[ExcludeFromCodeCoverage]
public sealed partial class ConflictItemViewModel : LocalizedObservableObject
{
    private readonly SyncConflict conflict;
    private readonly ISyncService syncService;
    private readonly ILocalizationService loc;

    public ConflictItemViewModel(SyncConflict conflict, ISyncService syncService, ILocalizationService loc) : base(loc)
    {
        this.conflict = conflict;
        this.syncService = syncService;
        this.loc = loc;
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, SelectedPolicy);
    }

    public Guid Id => conflict.Id;
    public string AccountId => conflict.Remote.AccountId.Value;
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

    /// <summary>Localised local file heading.</summary>
    public string LocalFileText => loc.GetLocal("Conflict.LocalFile");

    /// <summary>Localised remote file heading.</summary>
    public string RemoteFileText => loc.GetLocal("Conflict.RemoteFile");

    /// <summary>Localised separator between the local and remote file.</summary>
    public string VersusText => loc.GetLocal("Conflict.Versus");

    /// <summary>Localised resolution heading.</summary>
    public string ResolutionText => loc.GetLocal("Conflict.Resolution");

    /// <summary>Localised in-progress message.</summary>
    public string ResolvingText => loc.GetLocal("Conflict.Resolving");

    /// <summary>Localised dismiss button label.</summary>
    public string DismissText => loc.GetLocal("Conflict.Dismiss");

    /// <summary>Localised apply button label.</summary>
    public string ApplyText => loc.GetLocal("Conflict.Apply");

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
        if (IsResolving)
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

    /// <inheritdoc />
    protected override void OnCultureChanged()
    {
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, SelectedPolicy);
        base.OnCultureChanged();
    }

    private static string FormatDateTime(DateTimeOffset dt)
        => dt.LocalDateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture);
}
