using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDriveSync.old.Converters;

/// <summary>
/// Converts <c>IsIncluded</c> (bool) to a tooltip string.
/// <c>true</c> → "Stop syncing this folder"; <c>false</c> → "Sync this folder".
/// </summary>
public sealed class BoolToExcludeIncludeTooltipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Stop syncing this folder" : "Sync this folder";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
