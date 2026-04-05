using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Maps FolderSyncState to a badge background colour.</summary>
public sealed class SyncStateToBadgeBackgroundConverter : IValueConverter
{
    public static readonly SyncStateToBadgeBackgroundConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FolderSyncState state
            ? Color.Parse(state switch
            {
                FolderSyncState.Synced => "#EAF3DE",
                FolderSyncState.Syncing => "#E6F1FB",
                FolderSyncState.Included => "#E6F1FB",
                FolderSyncState.Partial => "#FAEEDA",
                FolderSyncState.Conflict => "#FAEEDA",
                FolderSyncState.Error => "#FCEBEB",
                _ => "#F1EFE8"
            })
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
