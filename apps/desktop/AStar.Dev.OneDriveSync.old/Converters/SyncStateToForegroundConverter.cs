using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AStar.Dev.OneDriveSync.old.Models;

namespace AStar.Dev.OneDriveSync.old.Converters;

/// <summary>Maps a <see cref="SyncState"/> to a text foreground colour.</summary>
public sealed class SyncStateToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SyncState state
            ? state switch
            {
                SyncState.Synced   => new SolidColorBrush(Color.Parse("#1D9E75")),
                SyncState.Syncing  => new SolidColorBrush(Color.Parse("#185FA5")),
                SyncState.Pending  => new SolidColorBrush(Color.Parse("#BA7517")),
                SyncState.Conflict => new SolidColorBrush(Color.Parse("#E24B4A")),
                SyncState.Error    => new SolidColorBrush(Color.Parse("#E24B4A")),
                SyncState.Excluded => new SolidColorBrush(Color.Parse("#888780")),
                _                  => new SolidColorBrush(Color.Parse("#1A1917"))
            }
            : new SolidColorBrush(Color.Parse("#1A1917"));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
