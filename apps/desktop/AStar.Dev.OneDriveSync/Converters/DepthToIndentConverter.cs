using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDriveSync.Converters;

/// <summary>Converts a tree depth (int) to an indentation width in pixels (depth × 16).</summary>
public sealed class DepthToIndentConverter : IValueConverter
{
    private const double IndentPerLevel = 16.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int depth ? depth * IndentPerLevel : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
