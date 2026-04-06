using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class ConflictItemViewModel(
    SyncConflict conflict,
    ISyncService syncService) : ObservableObject
{
    public Guid Id => conflict.Id;
    public string AccountId => conflict.AccountId;
    public string FileName => Path.GetFileName(conflict.RelativePath);
    public string RelativePath => conflict.RelativePath;
    public DateTimeOffset LocalModified => conflict.LocalModified;
    public DateTimeOffset RemoteModified => conflict.RemoteModified;
    public long LocalSize => conflict.LocalSize;
    public long RemoteSize => conflict.RemoteSize;
    public DateTimeOffset DetectedAt => conflict.DetectedAt;

    public string LocalModifiedText => FormatDateTime(conflict.LocalModified);
    public string RemoteModifiedText => FormatDateTime(conflict.RemoteModified);
    public string LocalSizeText => conflict.LocalSize.FileSizeToText();
    public string RemoteSizeText => conflict.RemoteSize.FileSizeToText();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelOpen))]
    private bool _isExpanded;

    public bool IsPanelOpen => IsExpanded;

    [ObservableProperty] private bool          _isResolving;
    [ObservableProperty] private bool          _isResolved;
    [ObservableProperty] private ConflictPolicy _selectedPolicy = ConflictPolicy.Ignore;

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; } =
    [
        new(ConflictPolicy.Ignore,        "Ignore",          "Skip — leave both versions unchanged"),
        new(ConflictPolicy.KeepBoth,      "Keep both",       "Rename local copy, keep remote"),
        new(ConflictPolicy.LastWriteWins, "Last write wins", "Most recently modified version wins"),
        new(ConflictPolicy.LocalWins,     "Local wins",      "Overwrite remote with local version"),
        new(ConflictPolicy.RemoteWins,    "Remote wins",     "Overwrite local with remote version"),
    ];

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

    private static string FormatDateTime(DateTimeOffset dt)
        => dt.LocalDateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture);
}

public sealed record ConflictPolicyOption(
    ConflictPolicy Policy,
    string Label,
    string Description);
