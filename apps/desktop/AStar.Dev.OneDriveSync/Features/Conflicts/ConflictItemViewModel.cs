using System;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.OneDriveSync.Infrastructure;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Conflicts;

/// <summary>Represents a single row in the Conflicts list (CR-06, CR-07).</summary>
public sealed class ConflictItemViewModel : ViewModelBase
{
    private readonly ConflictRecord _record;

    public ConflictItemViewModel(ConflictRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        _record = record;
    }

    /// <summary>Unique identifier of the underlying conflict.</summary>
    public Guid ConflictId => _record.Id;

    /// <summary>File name extracted from the full path (for display).</summary>
    public string FileName => System.IO.Path.GetFileName(_record.FilePath);

    /// <summary>Full local file path.</summary>
    public string FilePath => _record.FilePath;

    /// <summary>UTC timestamp when the local file was last modified.</summary>
    public DateTimeOffset LocalLastModified => _record.LocalLastModified;

    /// <summary>UTC timestamp when the remote file was last modified.</summary>
    public DateTimeOffset RemoteLastModified => _record.RemoteLastModified;

    /// <summary>Type of the conflict (BothModified / DeletedOnOneSide).</summary>
    public ConflictType ConflictType => _record.ConflictType;

    /// <summary>Display name of the owning account.</summary>
    public string AccountDisplayName => _record.AccountDisplayName;

    /// <summary>Whether this item is selected in the conflict list (CR-07).</summary>
    public bool IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}
