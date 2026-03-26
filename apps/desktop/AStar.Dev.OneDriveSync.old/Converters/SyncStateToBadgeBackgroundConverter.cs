using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AStar.Dev.OneDriveSync.old.Models;

namespace AStar.Dev.OneDriveSync.old.Converters;

/// <summary>Maps a <see cref="SyncState"/> to a badge background colour.</summary>
public sealed class SyncStateToBadgeBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SyncState state
            ? state switch
            {
                SyncState.Synced   => Color.Parse("#D4EDDA"),
                SyncState.Syncing  => Color.Parse("#CCE5FF"),
                SyncState.Pending  => Color.Parse("#FFF3CD"),
                SyncState.Conflict => Color.Parse("#F8D7DA"),
                SyncState.Error    => Color.Parse("#F8D7DA"),
                SyncState.Excluded => Color.Parse("#E2E3E5"),
                _                  => Color.Parse("#E2E3E5")
            }
            : Color.Parse("#E2E3E5");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
