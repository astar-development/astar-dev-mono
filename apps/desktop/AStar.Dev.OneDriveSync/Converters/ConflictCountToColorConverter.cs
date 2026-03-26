using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AStar.Dev.OneDriveSync.Converters;

/// <summary>Returns the conflict (red) colour when the count is greater than zero.</summary>
public sealed class ConflictCountToColorConverter : IValueConverter
{
    private static readonly Color Conflict = Color.Parse("#E24B4A");
    private static readonly Color Normal   = Color.Parse("#1A1917");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int n && n > 0 ? Conflict : Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
