using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AStar.Dev.OneDriveSync.Models;

namespace AStar.Dev.OneDriveSync.Converters;

/// <summary>Maps a <see cref="SyncState"/> to a badge text colour.</summary>
public sealed class SyncStateToBadgeForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SyncState state
            ? state switch
            {
                SyncState.Synced   => Color.Parse("#155724"),
                SyncState.Syncing  => Color.Parse("#004085"),
                SyncState.Pending  => Color.Parse("#856404"),
                SyncState.Conflict => Color.Parse("#721C24"),
                SyncState.Error    => Color.Parse("#721C24"),
                SyncState.Excluded => Color.Parse("#383D41"),
                _                  => Color.Parse("#383D41")
            }
            : Color.Parse("#383D41");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
