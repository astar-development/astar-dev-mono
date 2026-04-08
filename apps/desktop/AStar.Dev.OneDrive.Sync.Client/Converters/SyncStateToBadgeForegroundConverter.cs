using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Maps FolderSyncState to a badge text colour.</summary>
public sealed class SyncStateToBadgeForegroundConverter : IValueConverter
{
    public static readonly SyncStateToBadgeForegroundConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FolderSyncState state
            ? Color.Parse(state switch
            {
                FolderSyncState.Synced => "#27500A",
                FolderSyncState.Syncing => "#0C447C",
                FolderSyncState.Included => "#0C447C",
                FolderSyncState.Partial => "#633806",
                FolderSyncState.Conflict => "#633806",
                FolderSyncState.Error => "#791F1F",
                _ => "#5F5E5A"
            })
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
